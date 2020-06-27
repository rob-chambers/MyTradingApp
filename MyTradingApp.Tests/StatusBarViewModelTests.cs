using GalaSoft.MvvmLight.Messaging;
using MyTradingApp.EventMessages;
using MyTradingApp.ViewModels;
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

            var message = new AccountSummaryCompletedMessage
            {
                BuyingPower = BuyingPower,
                NetLiquidation = NetLiquidation
            };

            // Act
            Messenger.Default.Send(message);

            // Assert
            Assert.Equal("$500,000.00", vm.BuyingPower);
            Assert.Equal("$100,000.00", vm.NetLiquidation);
        }
    }
}
