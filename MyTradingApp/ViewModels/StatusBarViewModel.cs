using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Messaging;
using MyTradingApp.EventMessages;
using System.Globalization;

namespace MyTradingApp.ViewModels
{
    public class StatusBarViewModel : ViewModelBase
    {
        private string _connectionStatusText;
        private string _availableFunds;
        private string _buyingPower;

        public StatusBarViewModel()
        {
            Messenger.Default.Register<AccountSummaryMessage>(this, HandleAccountSummaryMessage);
        }

        private void HandleAccountSummaryMessage(AccountSummaryMessage args)
        {
            AvailableFunds = args.AvailableFunds.ToString("C", CultureInfo.GetCultureInfo("en-US"));
            BuyingPower = args.BuyingPower.ToString("C", CultureInfo.GetCultureInfo("en-US"));
        }

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
