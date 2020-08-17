using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Messaging;
using Microsoft.Extensions.Configuration;
using MyTradingApp.Core.EventMessages;
using MyTradingApp.Domain;
using System.Globalization;

namespace MyTradingApp.Core.ViewModels
{
    public class StatusBarViewModel : ViewModelBase
    {
        private readonly IConfiguration _configuration;
        private string _connectionStatusText = "Disconnected...";
        private string _netLiquidation;
        private string _buyingPower;

        public StatusBarViewModel(IConfiguration configuration)
        {
            Messenger.Default.Register<AccountSummaryMessage>(this, HandleAccountSummaryMessage);
            Messenger.Default.Register<ConnectionChangedMessage>(this, HandleConnectionChangedMessage);
            _configuration = configuration;
        }

        public string ConnectionStatusText
        {
            get => _connectionStatusText;
            set => Set(ref _connectionStatusText, value);
        }

        public string AccountTypeContent => $"Account Type: {AccountType}";

        public AccountType AccountType => _configuration.GetValue<AccountType>(Settings.AccountType);

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
