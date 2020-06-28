using GalaSoft.MvvmLight;

namespace MyTradingApp.ViewModels
{
    internal abstract class MenuItemViewModel : ViewModelBase
    {
        private object _icon;
        private object _label;
        private object _toolTip;
        private bool _isVisible = true;

        protected MenuItemViewModel(MainViewModel mainViewModel)
        {
            MainViewModel = mainViewModel;
        }

        public MainViewModel MainViewModel { get; }

        public object Icon
        {
            get => _icon;
            set => Set(ref _icon, value);
        }

        public object Label
        {
            get => _label;
            set => Set(ref _label, value);
        }

        public object ToolTip
        {
            get => _toolTip;
            set => Set(ref _toolTip, value);
        }

        public bool IsVisible
        {
            get => _isVisible;
            set => Set(ref _isVisible, value);
        }
    }
}
