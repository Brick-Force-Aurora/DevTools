using Avalonia.Media.Imaging;

namespace BrickForceDevTools.ViewModels
{
    public class ModeToggleItem : ViewModelBase
    {
        public string Name { get; set; } = "";
        public ushort Bit { get; set; }
        public Bitmap Icon { get; set; } = null!;

        private bool _isActive;
        public bool IsActive
        {
            get => _isActive;
            set => SetProperty(ref _isActive, value);
        }
    }
}
