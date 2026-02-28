using Avalonia.Controls;
using BrickForceDevTools;
using System.IO;
using System;
using System.Collections.ObjectModel;
using BrickForceDevTools.ViewModels;
using Avalonia.Interactivity;
using Newtonsoft.Json;
using System.Collections.Generic;
using Avalonia.Platform.Storage;
using Avalonia;
using Avalonia.VisualTree;
using MsBox.Avalonia.Dto;
using MsBox.Avalonia.Models;
using MsBox.Avalonia;
using System.Linq;
using System.Text.Json;
using System.Text;
using Tmds.DBus.Protocol;
using System.Reflection.PortableExecutable;
using System.Threading.Tasks;
using Avalonia.Threading;
using BrickForceDevTools.Patch;

namespace BrickForceDevTools.Views
{
    public partial class MainWindow : Window
    {
        private readonly MainWindowViewModel _viewModel;

        private readonly char crypt = 'E';

        private readonly PatchBuilderViewModel _patchVm = PatchBuilderViewModel.Instance;
        private System.Collections.Generic.List<PatchBuilder.DiffEntry> _currentDiff = new();

        public MainWindow(MainWindowViewModel vm)
        {
            InitializeComponent();
            Global.MainWindowInstance = this;
            _viewModel = vm;
            DataContext = _viewModel;
            if (File.Exists(".\\Assets\\bricks.json"))
            {
                string json = File.ReadAllText(".\\Assets\\bricks.json");
                RegMapManager.bricks = JsonConvert.DeserializeObject<List<Brick>>(json);
                Converter.BuildAliasToRE();
            }
            else
            {
                Global.PrintLine("Could not load bricks.json: " + Path.GetFullPath(".\\Assets\\bricks.json"));
            }
            LoadSettings();

            Global.RegMapCountChanged += count =>
            {
                Dispatcher.UIThread.Post(() =>
                {
                    TxtRegMapCount.Text = $"Loaded: {count}";
                });
            };

            // initialize label
            TxtRegMapCount.Text = $"Loaded: {Global.RegMapCount}";
        }

        public class AppSettings
        {
            public bool SkipMissingGeometry { get; set; } = true;
            public bool DefaultExportAll { get; set; } = false;
            public bool DefaultExportRegMap { get; set; } = true;
            public bool DefaultExportGeometry { get; set; } = true;
            public bool DefaultExportJson { get; set; } = true;
            public bool DefaultExportObj { get; set; } = true;
            public bool DefaultExportPlaintext { get; set; } = true;
            public bool IncludeAssemblyLineInPatchInfo { get; set; } = false;
            public string DefaultExportLocation { get; set; } = Path.GetFullPath("Export");
        }

        private void LoadSettings()
        {
            try
            {
                AppSettings settings;

                if (File.Exists(Global.settingsFilePath))
                {
                    var json = File.ReadAllText(Global.settingsFilePath);
                    settings = System.Text.Json.JsonSerializer.Deserialize<AppSettings>(json)
                               ?? new AppSettings();

                    Global.PrintLine("Loaded Settings from File.");
                }
                else
                {
                    settings = new AppSettings
                    {
                        DefaultExportLocation = Path.GetFullPath("Export")
                    };

                    Global.PrintLine("Initialized default settings.");
                }

                // Normalize path to full path
                if (string.IsNullOrWhiteSpace(settings.DefaultExportLocation))
                    settings.DefaultExportLocation = Path.GetFullPath("Export");
                else
                    settings.DefaultExportLocation = Path.GetFullPath(settings.DefaultExportLocation);

                // Update Global
                Global.SkipMissingGeometry = settings.SkipMissingGeometry;
                Global.DefaultExportAll = settings.DefaultExportAll;
                Global.DefaultExportRegMap = settings.DefaultExportRegMap;
                Global.DefaultExportGeometry = settings.DefaultExportGeometry;
                Global.DefaultExportJson = settings.DefaultExportJson;
                Global.DefaultExportObj = settings.DefaultExportObj;
                Global.DefaultExportPlaintext = settings.DefaultExportPlaintext;
                Global.IncludeAssemblyLineInPatchInfo = settings.IncludeAssemblyLineInPatchInfo;
                Global.DefaultExportLocation = settings.DefaultExportLocation;

                // Update the *actual* bound VM
                _viewModel.SkipMissingGeometry = settings.SkipMissingGeometry;
                _viewModel.DefaultExportAll = settings.DefaultExportAll;
                _viewModel.DefaultExportRegMap = settings.DefaultExportRegMap;
                _viewModel.DefaultExportGeometry = settings.DefaultExportGeometry;
                _viewModel.DefaultExportJson = settings.DefaultExportJson;
                _viewModel.DefaultExportObj = settings.DefaultExportObj;
                _viewModel.DefaultExportPlaintext = settings.DefaultExportPlaintext;
                _viewModel.IncludeAssemblyLineInPatchInfo = settings.IncludeAssemblyLineInPatchInfo;
                _viewModel.DefaultExportLocation = settings.DefaultExportLocation;
            }
            catch (Exception ex)
            {
                Global.PrintLine("Error loading settings: " + ex.Message);
            }
        }

        private async void OnLoadFileClick(object sender, RoutedEventArgs e)
        {
            var files = await this.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = "Select RegMap file(s)",
                AllowMultiple = true,
                FileTypeFilter = new[]
                {
            new FilePickerFileType("RegMap Files") { Patterns = new[] { "*.regmap" } }
        }
            });

            if (files.Count == 0)
                return;

            _viewModel.RegMapsViewModel.RegMaps.Clear();
            Global.ResetRegMapCount();

            // Convert once to local paths (avoid touching picker objects in background)
            var filePaths = files.Select(f => f.Path.LocalPath).ToList();

            await Task.Run(() =>
            {
                foreach (var filePath in filePaths)
                {
                    var geometryPath = filePath.Replace("regmap", "geometry");

                    if (Global.SkipMissingGeometry && !File.Exists(geometryPath))
                    {
                        Global.PrintLine($"Missing Geometry File: {Path.GetFileName(geometryPath)}");
                        continue;
                    }

                    var regMap = RegMapManager.Load(filePath);

                    Dispatcher.UIThread.Post(() =>
                    {
                        _viewModel.RegMapsViewModel.RegMaps.Add(regMap);
                    });

                    Global.IncrementRegMapCount();
                }
            });
        }

        private async void OnLoadFolderClick(object sender, RoutedEventArgs e)
        {
            var folders = await this.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
            {
                Title = "Select folder(s) containing RegMap files",
                AllowMultiple = true
            });

            if (folders.Count == 0)
                return;

            _viewModel.RegMapsViewModel.RegMaps.Clear();
            Global.ResetRegMapCount();

            // Convert once to local paths
            var folderPaths = folders.Select(f => f.Path.LocalPath).ToList();

            await Task.Run(() =>
            {
                foreach (var folderPath in folderPaths)
                {
                    foreach (var filePath in Directory.EnumerateFiles(folderPath, "*.regmap"))
                    {
                        var geometryPath = filePath.Replace("regmap", "geometry");

                        if (Global.SkipMissingGeometry && !File.Exists(geometryPath))
                        {
                            Global.PrintLine($"Missing Geometry File: {Path.GetFileName(geometryPath)}");
                            continue;
                        }

                        var regMap = RegMapManager.Load(filePath);

                        Dispatcher.UIThread.Post(() =>
                        {
                            _viewModel.RegMapsViewModel.RegMaps.Add(regMap);
                        });

                        Global.IncrementRegMapCount();
                    }
                }
            });
        }


        private async void OnRegMapSelected(object? sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count > 0 && e.AddedItems[0] is RegMap selectedRegMap)
            {
                _viewModel.SelectedRegMap = selectedRegMap;
            }
        }

        private async void OnTemplateFileSelected(object? sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count > 0 && e.AddedItems[0] is TemplateFile selectedTemplateFile)
            {
                _viewModel.SelectedTemplateFile = selectedTemplateFile;
            }
        }

        private void LoadRegMap(string filePath)
        {
            var regMap = RegMapManager.Load(filePath); // Assuming RegMapManager.Load returns a RegMap object
            if (regMap != null && regMap.map != -1)
            {
                _viewModel.RegMapsViewModel.RegMaps.Add(regMap);  // Add to ObservableCollection
                Global.PrintLine($"Loaded: {regMap.alias}");
            }
            else
            {
                Global.PrintLine($"Failed to load: {Path.GetFileName(filePath)}");
            }
        }

        private void OnExportAllClick(object? sender, RoutedEventArgs e)
        {
            var exportDialog = new ExportDialog
            {
                WindowStartupLocation = WindowStartupLocation.CenterScreen
            };

            exportDialog.ShowDialog(this);
        }

        private void OnSaveClick(object? sender, RoutedEventArgs e)
        {
            try
            {
                // normalize export path to full path
                var exportPath = string.IsNullOrWhiteSpace(_viewModel.DefaultExportLocation)
                    ? Path.GetFullPath("Export")
                    : Path.GetFullPath(_viewModel.DefaultExportLocation);

                // build settings snapshot (DTO)
                var settings = new AppSettings
                {
                    SkipMissingGeometry = _viewModel.SkipMissingGeometry,
                    DefaultExportAll = _viewModel.DefaultExportAll,
                    DefaultExportRegMap = _viewModel.DefaultExportRegMap,
                    DefaultExportGeometry = _viewModel.DefaultExportGeometry,
                    DefaultExportJson = _viewModel.DefaultExportJson,
                    DefaultExportObj = _viewModel.DefaultExportObj,
                    DefaultExportPlaintext = _viewModel.DefaultExportPlaintext,
                    IncludeAssemblyLineInPatchInfo = _viewModel.IncludeAssemblyLineInPatchInfo,
                    DefaultExportLocation = exportPath
                };

                var json = System.Text.Json.JsonSerializer.Serialize(
                    settings,
                    new System.Text.Json.JsonSerializerOptions { WriteIndented = true }
                );

                File.WriteAllText(Global.settingsFilePath, json);

                // Update global settings too (keep app consistent immediately)
                Global.SkipMissingGeometry = settings.SkipMissingGeometry;
                Global.DefaultExportAll = settings.DefaultExportAll;
                Global.DefaultExportRegMap = settings.DefaultExportRegMap;
                Global.DefaultExportGeometry = settings.DefaultExportGeometry;
                Global.DefaultExportJson = settings.DefaultExportJson;
                Global.DefaultExportObj = settings.DefaultExportObj;
                Global.DefaultExportPlaintext = settings.DefaultExportPlaintext;
                Global.IncludeAssemblyLineInPatchInfo = settings.IncludeAssemblyLineInPatchInfo;
                Global.DefaultExportLocation = settings.DefaultExportLocation;

                // Also push normalized path back into VM so UI shows full path
                _viewModel.DefaultExportLocation = settings.DefaultExportLocation;

                Global.PrintLine("Settings saved successfully.");
            }
            catch (Exception ex)
            {
                Global.PrintLine($"Error saving settings: {ex.Message}");
            }
        }

        private async void OnClearFoldersClick(object? sender, RoutedEventArgs e)
        {
            try
            {
                var folderPath = Path.GetFullPath("Resources\\Maps");
                var window = this.GetVisualRoot() as Window;

                if (window != null)
                {
                    var messageBox = MessageBoxManager.GetMessageBoxCustom(
                        new MessageBoxCustomParams
                        {
                            ButtonDefinitions = new List<ButtonDefinition>
                            {
                            new ButtonDefinition { Name = "Yes" },
                            new ButtonDefinition { Name = "No" },
                            },
                            ContentTitle = "Confirm Deletion",
                            ContentMessage = $"Are you sure you want to delete files in: {folderPath}?",

                            // Centering and Sizing
                            WindowStartupLocation = WindowStartupLocation.CenterOwner,
                            ShowInCenter = true,
                            CanResize = false,
                            MaxWidth = 500,
                            MaxHeight = 250,
                            SizeToContent = SizeToContent.WidthAndHeight,
                            Topmost = true,

                            // Background & Styling
                            WindowIcon = new WindowIcon("Assets/bf-logo.ico"), // Set a custom window icon
                        });
                    var result = await messageBox.ShowAsPopupAsync(window);
                    if (result != "Yes")
                    {
                        return; // User canceled deletion
                    }
                }

                string[] files = Directory.GetFiles(folderPath, "*.regmap", SearchOption.AllDirectories);
                string[] files2 = Directory.GetFiles(folderPath, "*.cache", SearchOption.AllDirectories);

                foreach (var file in files2.Concat(files))
                {
                    File.Delete(file);
                }

                Global.PrintLine("Selected folder cleaned successfully.");
            }
            catch (Exception ex)
            {
                Global.PrintLine($"Error clearing folders: {ex.Message}");
            }
        }

        private async void LoadFiles(object sender, RoutedEventArgs e)
        {
            var folders = await this.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
            {
                Title = "Select Folder Containing Template Files",
                AllowMultiple = false
            });

            if (folders.Count > 0)
            {
                var folderPath = folders[0].Path.LocalPath;
                var files = Directory.GetFiles(folderPath, "*.*", SearchOption.AllDirectories)
                                     .Select(f => new TemplateFile { FileName = Path.GetFileName(f), FileType = Path.GetExtension(f), FilePath = f })
                                     .ToList();
                _viewModel.RegMapsViewModel.TemplateFiles.Clear();
                foreach (var file in files)
                {
                    if (file.FileType == ".cooked")
                    {
                        UncookFile(file);
                    }
                    else if (file.FileType == ".txt")
                    {
                        file.Content = File.ReadAllLines(file.FilePath).Select(line => line.Split(',')).ToList();
                    }
                    _viewModel.RegMapsViewModel.TemplateFiles.Add(file);
                }
            }
        }

        private async void SaveCookedFile(object sender, RoutedEventArgs e)
        {
            var folders = await this.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
            {
                Title = "Select Folder To Save Cooked Template Files",
                AllowMultiple = false
            });

            foreach (TemplateFile file in _viewModel.RegMapsViewModel.TemplateFiles)
            {
                if (file.FileType == ".cooked")
                {
                    //copy file to new location
                    var targetPath = Path.Combine(folders[0].Path.LocalPath, file.FileName);
                    File.Copy(file.FilePath, targetPath, true);
                    Global.PrintLine($"Copied Cooked file: {targetPath}");
                } else if (file.FileType == ".txt")
                {
                    var targetPath = Path.Combine(folders[0].Path.LocalPath, file.FileName + ".cooked");
                    CookAndSaveFile(file, targetPath);
                }
            }
        }

        private async void SaveUncookedFile(object sender, RoutedEventArgs e)
        {
            var folders = await this.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
            {
                Title = "Select Folder To Save Uncooked Template Files",
                AllowMultiple = false
            });

            foreach (TemplateFile file in _viewModel.RegMapsViewModel.TemplateFiles)
            {
                if (file.FileType == ".cooked")
                {
                    var targetPath = Path.Combine(folders[0].Path.LocalPath, file.FileName.Replace(".cooked", ""));
                    File.WriteAllText(targetPath, string.Join("\n", file.Content.Select(row => string.Join(",", row))));
                    Global.PrintLine($"Saved Uncooked file: {targetPath}");
                }
                else if (file.FileType == ".txt")
                {
                    //copy file to new location
                    var targetPath = Path.Combine(folders[0].Path.LocalPath, file.FileName);
                    File.Copy(file.FilePath, targetPath, true);
                    Global.PrintLine($"Copied Uncooked file: {targetPath}");
                }
            }
        }

        private void UncookFile(TemplateFile file)
        {
            if (file == null) return;
            using (FileStream fileStream = File.Open(file.FilePath, FileMode.Open, FileAccess.Read))
            using (BinaryReader reader = new BinaryReader(fileStream))
            {
                int num = reader.ReadInt32();
                for (int i = 0; i < num; i++)
                {
                    int num2 = reader.ReadInt32();
                    string[] array = new string[num2];
                    for (int j = 0; j < num2; j++)
                    {
                        int num3 = reader.ReadInt32();
                        if (num3 > 0)
                        {
                            char[] array2 = reader.ReadChars(num3);
                            for (int k = 0; k < num3; k++)
                                array2[k] ^= crypt;
                            array[j] = new string(array2);
                        }
                        else
                        {
                            array[j] = string.Empty;
                        }
                    }
                    file.Content.Add(array);
                }
                reader.Close();
                fileStream.Close();
            }
            Global.PrintLine($"Uncooked file: {file.FilePath}");
        }

        private void CookAndSaveFile(TemplateFile file, string pathName)
        {
            if (file == null) return;
            using (FileStream fileStream = File.Open(pathName, FileMode.Create, FileAccess.Write))
            using (BinaryWriter binaryWriter = new BinaryWriter(fileStream))
            {
                binaryWriter.Write(file.Content.Count);
                foreach (var row in file.Content)
                {
                    binaryWriter.Write(row.Length);
                    foreach (var entry in row)
                    {
                        char[] array = entry.ToCharArray();
                        for (int k = 0; k < array.Length; k++)
                            array[k] ^= crypt;
                        binaryWriter.Write(array.Length);
                        binaryWriter.Write(array);
                    }
                }
                binaryWriter.Close();
                fileStream.Close();
            }
            Global.PrintLine($"Saved Cooked file: {pathName}");
        }

        private async void OnSelectFoldersForPatchClick(object? sender, RoutedEventArgs e)
        {
            // Pick A
            var a = await StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
            {
                Title = "Select Folder A (baseline)",
                AllowMultiple = false
            });
            if (a.Count == 0) return;

            // Pick B
            var b = await StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
            {
                Title = "Select Folder B (new)",
                AllowMultiple = false
            });
            if (b.Count == 0) return;

            var folderA = a[0].Path.LocalPath;
            var folderB = b[0].Path.LocalPath;

            _patchVm.FolderA = folderA;
            _patchVm.FolderB = folderB;
            _patchVm.Summary = "Computing diff…";
            _patchVm.CanBuildPatch = false;
            _patchVm.DiffItems.Clear();

            // Compute diff off UI thread
            _currentDiff = await Task.Run(() =>
                PatchBuilder.ComputeDiff(folderA, folderB, Global.SkipMissingGeometry, Global.PrintLine)
            );

            // Fill grid
            foreach (var d in _currentDiff)
            {
                _patchVm.DiffItems.Add(new PatchDiffItem
                {
                    BaseName = d.BaseName,
                    MapName = d.MapName,
                    Creator = d.Creator,
                    HasGeometry = d.HasGeometry,
                    RegMapFile = Path.GetFileName(d.RegMapPath),
                    GeometryFile = d.HasGeometry ? Path.GetFileName(d.GeometryPath) : ""
                });
            }

            _patchVm.Summary = $"New maps in B not in A: {_currentDiff.Count}";
            _patchVm.CanBuildPatch = _currentDiff.Count > 0;
        }

        private async void OnBuildPatchClick(object? sender, RoutedEventArgs e)
        {
            if (_currentDiff == null || _currentDiff.Count == 0)
            {
                Global.PrintLine("[Patch] No diff computed.");
                return;
            }

            var outFolderPick = await StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
            {
                Title = "Select output folder for patch",
                AllowMultiple = false
            });
            if (outFolderPick.Count == 0) return;

            var outFolder = outFolderPick[0].Path.LocalPath;

            _patchVm.Summary = "Building patch…";

            await Task.Run(() =>
            {
                PatchBuilder.BuildPatchOutput(_currentDiff, outFolder, Global.PrintLine);
            });

            _patchVm.Summary = $"Patch built into: {outFolder}";
        }
    }
}