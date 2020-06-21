using GalaSoft.MvvmLight;

namespace MyTradingApp.ViewModels
{
    public class StatusBarViewModel : ViewModelBase
    {
        private string _connectionStatusText;
        private string _availableFunds;
        private string _buyingPower;
        
        public string ConnectionStatusText
        {
            get => _connectionStatusText;
            set => Set(ref _connectionStatusText, value);
        }

        public string AvailableFunds
        {
            get => _availableFunds;
            set => Set(ref _availableFunds, value);
        }
        
        public string BuyingPower
        {
            get => _buyingPower;
            set => Set(ref _buyingPower, value);
        }        
    }
}
