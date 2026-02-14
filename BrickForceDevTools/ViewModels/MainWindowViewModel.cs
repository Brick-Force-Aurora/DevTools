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

            // IMPORTANT: replace ModeMask with the real property/field on your RegMap
            ushort mask = _selectedRegMap.modeMask; // or .ModeMask

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
