using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using BrickForceDevTools.ViewModels;
using BrickForceDevTools;
using System.IO;
using Newtonsoft.Json;
using System;
using Avalonia.Controls.Shapes;
using System.Threading.Tasks;
using Avalonia.Threading;
using System.Xml.Linq;

namespace BrickForceDevTools.Views;

public partial class ExportDialog : Window
{
    // Parameterless constructor for XAML designer support
    public ExportDialog()
    {
        InitializeComponent();
        _viewModel = new ExportDialogViewModel();  // Fallback for designer/XAML
        DataContext = _viewModel;
    }

    private readonly ExportDialogViewModel _viewModel;
    public ExportDialog(ExportDialogViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = _viewModel;
    }

    private string CreateExportDirectory(string baseDir, string name, int id)
    {
        Directory.CreateDirectory(baseDir);

        string path = System.IO.Path.Combine(baseDir, $"{name}-{id}");
        Directory.CreateDirectory(path);
        Directory.CreateDirectory(System.IO.Path.Combine(path, "OBJ"));

        return path;
    }

    private string FilterPathName(string name)
    {
        if (name == "" || name == " ")
            name = "empty";
        else
        {
            name = name.Replace('<', '_');
            name = name.Replace('>', '_');
            name = name.Replace(':', '_');
            name = name.Replace('\"', '_');
            name = name.Replace('/', '_');
            name = name.Replace('\\', '_');
            name = name.Replace('|', '_');
            name = name.Replace('?', '_');
            name = name.Replace('*', '_');
        }

        return name;
    }

    private string FixJSONFormat(string json)
    {
        //fix the format, not an ideal solution
        json = json.Replace("\":{\"", "\":\"");
        json = json.Replace("\"X\":", "\"X=");
        json = json.Replace("\"Y\":", "Y=");
        json = json.Replace("\"Z\":", "Z=");
        json = json.Replace("\"P\":", "\"P=");
        json = json.Replace("\"R\":", "R=");
        json = json.Replace(",R", " R");
        json = json.Replace(",Y", " Y");
        json = json.Replace(",Z", " Z");
        json = json.Replace("},\"", "\",\"");
        json = json.Replace("}}]", "\"}]");
        json = json.Replace("}},{\"", "\"},{\"");
        json = json.Remove(0, 1);
        return json;
    }

    private async void OnExportClick(object? sender, RoutedEventArgs e)
    {
        if (_viewModel.RegMapsViewModel.RegMaps.Count <= 0)
        {
            Global.PrintLine("No maps selected for export!");
        } else
        {
            ExportSpinner.IsActive = true; // Show loading indicator
            ExportSpinner.IsVisible = true;
            ButtonExport.IsEnabled = false;
            TextBlock.IsVisible = false;
            Global.PrintLine(_viewModel.RegMapsViewModel.RegMaps.Count + " maps will be exported!");
            await Task.Run(() =>
            {
                foreach (RegMap map in _viewModel.RegMapsViewModel.RegMaps)
                {
                    if (map == null)
                    {
                        continue;
                    }
                    if (_viewModel.ExportAll || map.isSelected)
                    {
                        if (_viewModel.ExportJson)
                        {
                            string json = JsonConvert.SerializeObject(Converter.RegMapToBFRE(map));
                            MapHeader mapData = new MapHeader(map);
                            string mapHeader = JsonConvert.SerializeObject(mapData);
                            mapHeader = mapHeader.Remove(0, 1);
                            json = FixJSONFormat(json);
                            string exportString = "{\"MapData\":{" + mapHeader + "," + json;
                            string name = FilterPathName(map.alias);
                            string path = CreateExportDirectory(Global.DefaultExportLocation, name, map.map);
                            File.WriteAllText(path + "\\" + name + ".json", exportString);
                            try
                            {
                                map.thumbnail.Save(path + "\\IMG-" + name + ".png");
                            }

                            catch (Exception ex)
                            {
                                Global.PrintLine("-------------------");
                                Global.PrintLine("Could not save thumbnail");
                                Global.PrintLine("-------------------");
                            }

                            Global.PrintLine("Path: " + path);
                        }

                        if (_viewModel.ExportObj)
                        {
                            string obj = Converter.RegMapToOBJ(map);
                            string name = FilterPathName(map.alias);
                            int id = map.map;

                            string path = CreateExportDirectory(Global.DefaultExportLocation, name, id);

                            //save textures
                            Global.Print("Saved textures: [");
                            for (int i = 0; i < ObjParser.referencedTextures.Count; i++)
                            {
                                File.WriteAllBytes(path + "\\" + ObjParser.referencedTextures[i].name, ObjParser.referencedTextures[i].data);
                                if (i < ObjParser.referencedTextures.Count - 1)
                                {
                                    Global.Print(ObjParser.referencedTextures[i].name + ", ");
                                } else
                                {
                                    Global.PrintLine(ObjParser.referencedTextures[i].name + "]");
                                }
                            }

                            //save mtl
                            File.WriteAllText(path + "\\" + name + ".mtl", ObjParser.mtlString);
                            Global.PrintLine("Saved " + name + ".mtl");

                            //save obj
                            File.WriteAllText(path + "\\" + name + ".obj", obj);
                            Global.PrintLine("Saved " + name + ".obj");

                            Global.PrintLine("Path: " + path);

                            //clean up
                            GC.Collect();
                            ObjParser.referencedTextures.Clear();
                            ObjParser.loadedObjs.Clear();
                        }

                        if (_viewModel.ExportRegMap)
                        {
                            string path = CreateExportDirectory(Global.DefaultExportLocation, map.alias, map.map);
                            if (map.Save(System.IO.Path.Combine(path, "downloaded" + map.map + ".regmap")))
                            {
                                Global.PrintLine("Saved downloaded" + map.map + ".regmap");
                            }
                            else
                            {
                                Global.PrintLine("Failed to save downloaded" + map.map + ".regmap");
                            }
                        }

                        if (_viewModel.ExportGeometry)
                        {
                            Global.PrintLine("Exporting Geometry is currently not supported.");
                        }

                        if (_viewModel.Plaintext)
                        {
                            string plaintext = Converter.RegMapToPlaintext(map);
                            string name = FilterPathName(map.alias);
                            string path = CreateExportDirectory(Global.DefaultExportLocation, name, map.map);
                            File.WriteAllText(path + "\\" + name + ".txt", plaintext);
                            Global.PrintLine("Saved " + name + ".txt");

                            Global.PrintLine("Path: " + path);

                        }
                    }
                }
            });

            ExportSpinner.IsActive = false;  // Hide loading indicator after export\
            ExportSpinner.IsVisible = false;
            ButtonExport.IsEnabled = true;
            TextBlock.IsVisible = true;
            this.Close();
        }
    }
}