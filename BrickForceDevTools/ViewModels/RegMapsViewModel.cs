using BrickForceDevTools;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BrickForceDevTools.ViewModels
{
    public class RegMapsViewModel: ViewModelBase
    {
        private static RegMapsViewModel _instance;
        public static RegMapsViewModel Instance => _instance ??= new RegMapsViewModel();

        public ObservableCollection<ModeToggleItem> ModeToggles { get; } = new();

        private ObservableCollection<RegMap> _regMaps = new ObservableCollection<RegMap>();
        public ObservableCollection<RegMap> RegMaps
        {
            get => _regMaps;
            set
            {
                _regMaps = value;
                OnPropertyChanged(nameof(RegMaps));
            }
        }

        private ObservableCollection<TemplateFile> _templateFiles = new ObservableCollection<TemplateFile>();
        public ObservableCollection<TemplateFile> TemplateFiles
        {
            get => _templateFiles;
            set
            {
                _templateFiles = value;
                OnPropertyChanged(nameof(TemplateFiles));
            }
        }

        private RegMap? _selectedRegMap;
        public RegMap? SelectedRegMap
        {
            get => _selectedRegMap;
            set => SetProperty(ref _selectedRegMap, value);
        }

        private string _mapName;
        public string MapName
        {
            get => _mapName;
            set
            {
                if (SetProperty(ref _mapName, value))
                {
                    if (SelectedRegMap != null)
                        SelectedRegMap.alias = value;
                }
            }
        }

        private bool _isSelected;
        public bool IsSelected
        {
            get => _isSelected;
            set => SetProperty(ref _isSelected, value);
        }

        private string _mapId;
        public string MapId
        {
            get => _mapId;
            set
            {
                if (SetProperty(ref _mapId, value))
                {
                    if (SelectedRegMap != null && int.TryParse(value, out var id))
                        SelectedRegMap.map = id;
                }
            }
        }

        private Avalonia.Media.Imaging.Bitmap _thumbnail;
        public Avalonia.Media.Imaging.Bitmap Thumbnail
        {
            get => _thumbnail;
            set => SetProperty(ref _thumbnail, value);
        }

        private string _creator;
        public string Creator
        {
            get => _creator;
            set
            {
                if (SetProperty(ref _creator, value))
                {
                    if (SelectedRegMap != null)
                        SelectedRegMap.developer = value;
                }
            }
        }

        private string _date;
        public string Date
        {
            get => _date;
            set => SetProperty(ref _date, value);
        }

        private int _brickCount;
        public int BrickCount
        {
            get => _brickCount;
            set => SetProperty(ref _brickCount, value);
        }

        private string _modes;
        public string Modes
        {
            get => _modes;
            set => SetProperty(ref _modes, value);
        }

        private bool _isOfficial;
        public bool IsOfficial
        {
            get => _isOfficial;
            set => SetProperty(ref _isOfficial, value);
        }

        private bool _isClan;
        public bool IsClan
        {
            get => _isClan;
            set => SetProperty(ref _isClan, value);
        }

        private RegMapsViewModel() {
            ModeToggles.Add(new ModeToggleItem { Name = "Team", Bit = (ushort)MODE_MASK.TEAM_MATCH_MASK, Icon = ModeIconDecoder.GetBitmap("avares://BrickForceDevTools/Assets/Modes/icon_teamMode.png") });
            ModeToggles.Add(new ModeToggleItem { Name = "Individual", Bit = (ushort)MODE_MASK.INDIVIDUAL_MATCH_MASK, Icon = ModeIconDecoder.GetBitmap("avares://BrickForceDevTools/Assets/Modes/icon_survivalMode.png") });
            ModeToggles.Add(new ModeToggleItem { Name = "CTF", Bit = (ushort)MODE_MASK.CAPTURE_THE_FALG_MATCH, Icon = ModeIconDecoder.GetBitmap("avares://BrickForceDevTools/Assets/Modes/icon_ctfMode.png") });
            ModeToggles.Add(new ModeToggleItem { Name = "Blast", Bit = (ushort)MODE_MASK.EXPLOSION_MATCH, Icon = ModeIconDecoder.GetBitmap("avares://BrickForceDevTools/Assets/Modes/icon_blastMode.png") });
            ModeToggles.Add(new ModeToggleItem { Name = "Mission", Bit = (ushort)MODE_MASK.MISSION_MASK, Icon = ModeIconDecoder.GetBitmap("avares://BrickForceDevTools/Assets/Modes/icon_defenceMode.png") });
            ModeToggles.Add(new ModeToggleItem { Name = "BND", Bit = (ushort)MODE_MASK.BND_MASK, Icon = ModeIconDecoder.GetBitmap("avares://BrickForceDevTools/Assets/Modes/icon_BND.png") });
            ModeToggles.Add(new ModeToggleItem { Name = "Bungee", Bit = (ushort)MODE_MASK.BUNGEE_MASK, Icon = ModeIconDecoder.GetBitmap("avares://BrickForceDevTools/Assets/Modes/icon_bungeeMode.png") });
            ModeToggles.Add(new ModeToggleItem { Name = "Escape", Bit = (ushort)MODE_MASK.ESCAPE_MASK, Icon = ModeIconDecoder.GetBitmap("avares://BrickForceDevTools/Assets/Modes/icon_runMode.png") });
            ModeToggles.Add(new ModeToggleItem { Name = "Zombie", Bit = (ushort)MODE_MASK.ZOMBIE_MASK, Icon = ModeIconDecoder.GetBitmap("avares://BrickForceDevTools/Assets/Modes/icon_zombieMode.png") });
        }
    }
}
