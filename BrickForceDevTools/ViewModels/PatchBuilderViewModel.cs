using System;
using System.Collections.ObjectModel;

namespace BrickForceDevTools.ViewModels
{
    public class PatchBuilderViewModel : ViewModelBase
    {
        private static PatchBuilderViewModel _instance;
        public static PatchBuilderViewModel Instance => _instance ??= new PatchBuilderViewModel();

        private string? _folderA;
        public string? FolderA
        {
            get => _folderA;
            set => SetProperty(ref _folderA, value);
        }

        private string? _folderB;
        public string? FolderB
        {
            get => _folderB;
            set => SetProperty(ref _folderB, value);
        }

        private string _summary = "Select Folder A and Folder B.";
        public string Summary
        {
            get => _summary;
            set => SetProperty(ref _summary, value);
        }

        private bool _canBuildPatch;
        public bool CanBuildPatch
        {
            get => _canBuildPatch;
            set => SetProperty(ref _canBuildPatch, value);
        }

        public ObservableCollection<PatchDiffItem> DiffItems { get; } = new();

        private PatchBuilderViewModel() { }
    }

    public class PatchDiffItem : ViewModelBase
    {
        public string BaseName { get; set; } = "";
        public string RegMapFile { get; set; } = "";
        public string GeometryFile { get; set; } = "";
        public bool HasGeometry { get; set; }
        public string MapName { get; set; } = "";
        public string Creator { get; set; } = "";
    }
}
