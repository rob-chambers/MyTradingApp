using MyTradingApp.Core.Repositories;
using MyTradingApp.Core.ViewModels;
using MyTradingApp.Domain;
using NSubstitute;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace MyTradingApp.Tests
{
    public class SettingsViewModelTests
    {
        [Fact]
        public void WhenSettingNotInRepositoryAdd()
        {
            // Arrange
            var repository = Substitute.For<ISettingsRepository>();
            var vm = new SettingsViewModel(repository);

            // Act
            vm.LastRiskMultiplier = 2;

            // Assert
            repository.Received().Add(Arg.Is<Setting>(x => x.Key == SettingsViewModel.SettingsKeys.RiskMultiplier && x.Value == "2"));
            repository.Received().SaveAsync();
        }

        [Fact]
        public async Task WhenSettingAlreadyLoadedUpdate()
        {
            // Arrange
            var repository = Substitute.For<ISettingsRepository>();
            repository.GetAllAsync().Returns(new List<Setting>
            {
                new Setting { Key = SettingsViewModel.SettingsKeys.RiskMultiplier, Value = "2" }
            });
            var vm = new SettingsViewModel(repository);
            await vm.LoadSettingsAsync();

            // Act
            vm.LastRiskMultiplier = 3;

            // Wait for save on background task to complete
            await Task.Delay(20);

            // Assert
            repository.Received().Update(Arg.Is<Setting>(x => x.Key == SettingsViewModel.SettingsKeys.RiskMultiplier && x.Value == "3"));
            await repository.Received().SaveAsync();
        }

        [Fact]
        public void WhenRiskPercentOfAccountSizeSettingNotInRepositoryAdd()
        {
            // Arrange
            var repository = Substitute.For<ISettingsRepository>();
            var vm = new SettingsViewModel(repository);

            // Act
            vm.RiskPercentOfAccountSize = 0.5;

            // Assert
            repository.Received().Add(Arg.Is<Setting>(x => x.Key == SettingsViewModel.SettingsKeys.RiskPercentOfAccountSize && x.Value == "0.5"));
            repository.Received().SaveAsync();
        }

        [Fact]
        public async Task WhenRiskPercentOfAccountSizeSettingAlreadyLoadedUpdate()
        {
            // Arrange
            var repository = Substitute.For<ISettingsRepository>();
            repository.GetAllAsync().Returns(new List<Setting>
            {
                new Setting { Key = SettingsViewModel.SettingsKeys.RiskPercentOfAccountSize, Value = "2" }
            });
            var vm = new SettingsViewModel(repository);
            await vm.LoadSettingsAsync();

            // Act
            vm.RiskPercentOfAccountSize = 3;

            // Wait a little while for the secondary task to run
            await Task.Delay(50);

            // Assert
            repository.Received().Update(Arg.Is<Setting>(x => x.Key == SettingsViewModel.SettingsKeys.RiskPercentOfAccountSize && x.Value == "3"));
            await repository.Received().SaveAsync();
        }

        [Fact]
        public async Task DefaultsAreCorrect()
        {
            // Arrange
            var repository = Substitute.For<ISettingsRepository>();
            repository.GetAllAsync().Returns(new List<Setting>());
            var vm = new SettingsViewModel(repository);
            await vm.LoadSettingsAsync();

            // Assert
            Assert.Equal(0.5, vm.RiskPercentOfAccountSize);
            Assert.Equal(1, vm.LastRiskMultiplier);
        }
    }
}
