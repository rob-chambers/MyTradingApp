using MyTradingApp.Core.Repositories;
using MyTradingApp.Core.Services;
using MyTradingApp.Core.ViewModels;
using MyTradingApp.Domain;
using NSubstitute;
using System.Threading.Tasks;
using Xunit;

namespace MyTradingApp.Tests
{
    public class RiskCalculationServiceTests
    {
        [Fact]
        public async Task ModifyingRiskPercentageOfAccountSizeModifiesRiskPerTrade()
        {
            var settingsRepository = Substitute.For<ISettingsRepository>();
            var exchangeRateService = Substitute.For<IExchangeRateService>();
            exchangeRateService.GetExchangeRateAsync().Returns(1);

            var accountManager = Substitute.For<IAccountManager>();
            accountManager.RequestAccountSummaryAsync().Returns(new AccountSummary
            {
                NetLiquidation = 10000                
            });

            var settingsViewModel = new SettingsViewModel(settingsRepository)
            {
                LastRiskMultiplier = 1,
                RiskPercentOfAccountSize = 1
            };

            var service = new RiskCalculationService(exchangeRateService, accountManager, settingsViewModel);

            // Act
            await service.RequestDataForCalculationAsync();

            // Assert
            Assert.Equal(100, service.RiskPerTrade);
            settingsViewModel.RiskPercentOfAccountSize = 2;
            Assert.Equal(200, service.RiskPerTrade);
        }

        [Fact]
        public void WhenAccountSummaryNotAvailableRiskPerTradeIsCorrect()
        {
            var settingsRepository = Substitute.For<ISettingsRepository>();
            var exchangeRateService = Substitute.For<IExchangeRateService>();
            var accountManager = Substitute.For<IAccountManager>();
            var settingsViewModel = new SettingsViewModel(settingsRepository);
            
            // Act
            var service = new RiskCalculationService(exchangeRateService, accountManager, settingsViewModel);

            // Assert
            Assert.Equal(0, service.RiskPerTrade);
        }
    }
}
