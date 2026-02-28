using BrickForceDevTools;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using System.Text;
using CommunityToolkit.Mvvm.Input;
using Newtonsoft.Json.Linq;

namespace BrickForceDevTools.ViewModels
{
    public partial class MainWindowViewModel : ViewModelBase
    {
        public RegMapsViewModel RegMapsViewModel { get; } = RegMapsViewModel.Instance;
        public PatchBuilderViewModel PatchBuilder => PatchBuilderViewModel.Instance;


        private RegMap _selectedRegMap;
        public RegMap SelectedRegMap
        {
            get => _selectedRegMap;
            set
            {
                if (SetProperty(ref _selectedRegMap, value))
                {
                    RegMapsViewModel.SelectedRegMap = value;
                    UpdateMapDetails(value);
                    UpdateModeTogglesFromSelected();
                }
            }
        }

        private void UpdateMapDetails(RegMap selectedRegMap)
        {
            if (selectedRegMap != null)
            {
                RegMapsViewModel.MapName = selectedRegMap.alias;
                RegMapsViewModel.MapId = selectedRegMap.map.ToString();
                RegMapsViewModel.Creator = selectedRegMap.developer;
                RegMapsViewModel.Date = selectedRegMap.regDate.ToString();
                RegMapsViewModel.BrickCount = selectedRegMap.geometry.brickCount;
                RegMapsViewModel.Modes = selectedRegMap.modeMask.ToString();
                RegMapsViewModel.IsOfficial = selectedRegMap.officialMap;
                RegMapsViewModel.IsClan = selectedRegMap.clanMatchable;
                RegMapsViewModel.Thumbnail = selectedRegMap.thumbnail;
            }
        }

        private void UpdateModeTogglesFromSelected()
        {
            if (_selectedRegMap == null)
            {
                foreach (var t in RegMapsViewModel.ModeToggles)
                    t.IsActive = false;
                return;
            }

            ushort mask = _selectedRegMap.modeMask;

            foreach (var t in RegMapsViewModel.ModeToggles)
                t.IsActive = (mask & t.Bit) != 0;
        }


        [ObservableProperty] private bool skipMissingGeometry = true;
        [ObservableProperty] private bool defaultExportAll = true;
        [ObservableProperty] private bool defaultExportRegMap = true;
        [ObservableProperty] private bool defaultExportGeometry = true;
        [ObservableProperty] private bool defaultExportJson = true;
        [ObservableProperty] private bool defaultExportObj = true;
        [ObservableProperty] private bool defaultExportPlaintext = true;
        [ObservableProperty] private bool includeAssemblyLineInPatchInfo = false;
        [ObservableProperty] private string defaultExportLocation = Path.GetFullPath("Export");

        partial void OnDefaultExportLocationChanged(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                value = "Export";

            var full = Path.GetFullPath(value);

            full = full.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

            defaultExportLocation = full;
            OnPropertyChanged(nameof(DefaultExportLocation));

            Global.DefaultExportLocation = full;
        }

        [RelayCommand]
        private async Task BrowseExportLocation()
        {
            // You already keep this around:
            var window = Global.MainWindowInstance;
            if (window == null) return;

            var top = TopLevel.GetTopLevel(window);
            if (top?.StorageProvider == null) return;

            var folders = await top.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
            {
                Title = "Select export folder",
                AllowMultiple = false
            });

            var picked = folders?.FirstOrDefault();
            if (picked == null) return;

            // Convert to local file path
            var path = picked.TryGetLocalPath();
            if (string.IsNullOrWhiteSpace(path)) return;

            // Setting this triggers OnDefaultExportLocationChanged -> normalizes + syncs Global
            DefaultExportLocation = path;
        }

        private TemplateFile _selectedTemplateFile;
        public TemplateFile SelectedTemplateFile
        {
            get => _selectedTemplateFile;
            set
            {
                if (SetProperty(ref _selectedTemplateFile, value))
                {
                    if (_selectedTemplateFile != null)
                    {
                        PreviewText = string.Join("\n", _selectedTemplateFile.Content.Select(row => string.Join(",", row)));
                    }
                }
            }
        }

        private string _previewText;

        public string PreviewText
        {
            get => _previewText;
            set
            {
                if (_previewText != value)
                {
                    _previewText = value;
                    OnPropertyChanged(nameof(PreviewText));
                }
            }
        }
    }
}
