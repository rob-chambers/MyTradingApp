using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Messaging;
using MyTradingApp.Core.EventMessages;
using System.Globalization;

namespace MyTradingApp.Core.ViewModels
{
    public class StatusBarViewModel : ViewModelBase
    {
        private string _connectionStatusText = "Disconnected...";
        private string _netLiquidation;
        private string _buyingPower;

        public StatusBarViewModel()
        {
            Messenger.Default.Register<AccountSummaryMessage>(this, HandleAccountSummaryMessage);
            Messenger.Default.Register<ConnectionChangedMessage>(this, HandleConnectionChangedMessage);
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

        private void HandleAccountSummaryMessage(AccountSummaryMessage message)
        {
            var summary = message.AccountSummary;
            NetLiquidation = summary.NetLiquidation.ToString("C", CultureInfo.GetCultureInfo("en-US"));
            BuyingPower = summary.BuyingPower.ToString("C", CultureInfo.GetCultureInfo("en-US"));
        }

        private void HandleConnectionChangedMessage(ConnectionChangedMessage message)
        {
            ConnectionStatusText = message.IsConnected
                ? "Connected to TWS"
                : "Disconnected...";
        }
    }
}
