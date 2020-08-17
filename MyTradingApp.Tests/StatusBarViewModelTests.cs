using GalaSoft.MvvmLight.Messaging;
using Microsoft.Extensions.Configuration;
using MyTradingApp.Core.EventMessages;
using MyTradingApp.Core.ViewModels;
using MyTradingApp.Domain;
using NSubstitute;
using Xunit;

namespace MyTradingApp.Tests
{
    public class StatusBarViewModelTests
    {
        private static StatusBarViewModel GetVm(IConfiguration configuration = null)
        {
            var config = configuration ?? Substitute.For<IConfiguration>();
            var vm = new StatusBarViewModel(config);
            return vm;
        }

        [Fact]
        public void WhenAccountSummaryRetrievedUpdateStatusBar()
        {
            const double BuyingPower = 500 * 1000;
            const double NetLiquidation = 100 * 1000;

            var vm = GetVm();

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
            var vm = GetVm();
            Assert.Equal("Disconnected...", vm.ConnectionStatusText);
        }

        [Fact]
        public void ConnectionStatusChangedOnceConnected()
        {
            var vm = GetVm();
            Messenger.Default.Send(new ConnectionChangedMessage(true));
            Assert.Equal("Connected to TWS", vm.ConnectionStatusText);
        }

        [Fact]
        public void ConnectionStatusChangedOnceDisconnected()
        {
            var vm = GetVm();
            Messenger.Default.Send(new ConnectionChangedMessage(true));
            Messenger.Default.Send(new ConnectionChangedMessage(false));
            Assert.Equal("Disconnected...", vm.ConnectionStatusText);
        }

        [Theory]
        [InlineData("Paper", AccountType.Paper)]
        [InlineData("Real", AccountType.Real)]
        public void AccountTypeSetCorrectly(string accountType, AccountType expected)
        {
            // Arrange
            var config = Substitute.For<IConfiguration>();
            var section = Substitute.For<IConfigurationSection>();
            section.Value.Returns(accountType);
            config.GetSection(Settings.AccountType).Returns(section);

            // Act
            var vm = GetVm(config);

            // Assert
            Assert.Equal(expected, vm.AccountType);
            Assert.Equal($"Account Type: {expected}", vm.AccountTypeContent);
        }
    }
}
