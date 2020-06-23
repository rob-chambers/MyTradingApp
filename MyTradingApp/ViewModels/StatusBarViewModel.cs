using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Messaging;
using MyTradingApp.EventMessages;
using System.Globalization;

namespace MyTradingApp.ViewModels
{
    public class StatusBarViewModel : ViewModelBase
    {
        private string _connectionStatusText;
        private string _netLiquidation;
        private string _buyingPower;

        public StatusBarViewModel()
        {
            Messenger.Default.Register<AccountSummaryCompletedMessage>(this, HandleAccountSummaryMessage);
        }

        private void HandleAccountSummaryMessage(AccountSummaryCompletedMessage args)
        {
            NetLiquidation = args.NetLiquidation.ToString("C", CultureInfo.GetCultureInfo("en-US"));
            BuyingPower = args.BuyingPower.ToString("C", CultureInfo.GetCultureInfo("en-US"));
        }

        public string ConnectionStatusText
        {
            get => _connectionStatusText;
            set => Set(ref _connectionStatusText, value);
        }

        public string NetLiquidation
        {
            get => _netLiquidation;
            set => Set(ref _netLiquidation, value);
        }
        
        public string BuyingPower
        {
            get => _buyingPower;
            set => Set(ref _buyingPower, value);
        }        
    }
}
