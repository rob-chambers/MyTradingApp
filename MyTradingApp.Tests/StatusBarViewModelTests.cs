using GalaSoft.MvvmLight.Messaging;
using MyTradingApp.Core.EventMessages;
using MyTradingApp.Core.ViewModels;
using MyTradingApp.Domain;
using Xunit;

namespace MyTradingApp.Tests
{
    public class StatusBarViewModelTests
    {
        [Fact]
        public void WhenAccountSummaryRetrievedUpdateStatusBar()
        {
            const double BuyingPower = 500 * 1000;
            const double NetLiquidation = 100 * 1000;

            var vm = new StatusBarViewModel();

            var summary = new AccountSummary
            {
                BuyingPower = BuyingPower,
                NetLiquidation = NetLiquidation
            };

            // Act
            Messenger.Default.Send(new AccountSummaryMessage(summary));

            // Assert
            Assert.Equal("$500,000.00", vm.BuyingPower);
            Assert.Equal("$100,000.00", vm.NetLiquidation);
        }

        [Fact]
        public void ConnectionStatusInitiallyCorrect()
        {
            var vm = new StatusBarViewModel();
            Assert.Equal("Disconnected...", vm.ConnectionStatusText);
        }

        [Fact]
        public void ConnectionStatusChangedOnceConnected()
        {
            var vm = new StatusBarViewModel();
            Messenger.Default.Send(new ConnectionChangedMessage(true));
            Assert.Equal("Connected to TWS", vm.ConnectionStatusText);
        }

        [Fact]
        public void ConnectionStatusChangedOnceDisconnected()
        {
            var vm = new StatusBarViewModel();
            Messenger.Default.Send(new ConnectionChangedMessage(true));
            Messenger.Default.Send(new ConnectionChangedMessage(false));
            Assert.Equal("Disconnected...", vm.ConnectionStatusText);
        }
    }
}
