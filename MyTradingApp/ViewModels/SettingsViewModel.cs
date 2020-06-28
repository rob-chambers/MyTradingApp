namespace MyTradingApp.ViewModels
{
    internal class SettingsViewModel : MenuItemViewModel
    {
        public SettingsViewModel(MainViewModel mainViewModel)
            : base(mainViewModel)
        {
        }

        public string Message => "Hello, world";
    }
}
