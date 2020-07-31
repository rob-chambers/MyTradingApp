using AutoFinance.Broker.InteractiveBrokers.EventArgs;
using GalaSoft.MvvmLight.Messaging;
using MyTradingApp.Core;
using MyTradingApp.Core.Repositories;
using MyTradingApp.Core.Utils;
using MyTradingApp.Domain;
using MyTradingApp.EventMessages;
using MyTradingApp.Repositories;
using MyTradingApp.Services;
using MyTradingApp.ViewModels;
using NSubstitute;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace MyTradingApp.Tests
{
    public class MainViewModelTests
    {
        private IConnectionService _connectionService;
        private IOrderManager _orderManager;
        private IAccountManager _accountManager;
        private IContractManager _contractManager;
        private IMarketDataManager _marketDataManager;
        private IHistoricalDataManager _historicalDataManager;
        private IOrderCalculationService _orderCalculationService;
        private IExchangeRateService _exchangeRateService;
        private ITradeRepository _tradeRepository;
        private ISettingsRepository _settingsRepository;
        private OrdersViewModel _ordersViewModel;
        private StatusBarViewModel _statusBarViewModel;
        private SettingsViewModel _settingsViewModel;

        private MainViewModel GetVm(ISettingsRepository settingsRepository = null)
        {
            MainViewModel.IsUnitTesting = true;
            _connectionService = Substitute.For<IConnectionService>();
            _orderManager = Substitute.For<IOrderManager>();
            _accountManager = Substitute.For<IAccountManager>();
            _contractManager = Substitute.For<IContractManager>();
            _marketDataManager = Substitute.For<IMarketDataManager>();
            _historicalDataManager = Substitute.For<IHistoricalDataManager>();
            _orderCalculationService = Substitute.For<IOrderCalculationService>();
            _exchangeRateService = Substitute.For<IExchangeRateService>();
            _tradeRepository = Substitute.For<ITradeRepository>();

            if (settingsRepository == null)
            {
                _settingsRepository = Substitute.For<ISettingsRepository>();
                _settingsRepository.GetAllAsync().Returns(new List<Setting>
                {
                    new Setting
                    {
                        Key = SettingsViewModel.SettingsKeys.RiskPercentOfAccountSize,
                        Value = "1"
                    },
                    new Setting
                    {
                        Key = SettingsViewModel.SettingsKeys.RiskMultiplier,
                        Value = "1"
                    }
                });
            }
            else
            {
                _settingsRepository = settingsRepository;
            }

            var orderManager = Substitute.For<IOrderManager>();

            var dispatcherHelper = Substitute.For<IDispatcherHelper>();
            dispatcherHelper
                .When(x => x.InvokeOnUiThread(Arg.Any<Action>()))
                .Do(x => x.Arg<Action>().Invoke());

            var queueProcessor = Substitute.For<IQueueProcessor>();
            queueProcessor
                .When(x => x.Enqueue(Arg.Any<Action>()))
                .Do(x => x.Arg<Action>().Invoke());

            _ordersViewModel = new OrdersViewModel(dispatcherHelper, _contractManager, _marketDataManager, _historicalDataManager, _orderCalculationService, orderManager, _tradeRepository, queueProcessor);
            _statusBarViewModel = Substitute.For<StatusBarViewModel>();

            var positionsManager = Substitute.For<IPositionManager>();
            var contractManager = Substitute.For<IContractManager>();            

            var positionsViewModel = new PositionsViewModel(dispatcherHelper, _marketDataManager, _accountManager, positionsManager, contractManager, queueProcessor);
            _settingsViewModel = new SettingsViewModel(_settingsRepository);            

            return new MainViewModel(dispatcherHelper, _connectionService, _orderManager, _accountManager, _ordersViewModel, _statusBarViewModel, _exchangeRateService, _orderCalculationService, positionsViewModel, _settingsViewModel, queueProcessor);
        }

        [Fact]
        public void ConnectionStatusInitiallyCorrect()
        {
            var vm = GetVm();
            Assert.Equal("Connect", vm.ConnectButtonCaption);
            Assert.False(vm.IsEnabled);
            Assert.Equal("Disconnected...", _statusBarViewModel.ConnectionStatusText);
        }

        [Fact]
        public async Task WhenConnectionErrorThenShowErrorInTextBox()
        {
            var vm = GetVm();
            _connectionService
                .When(x => x.ConnectAsync())
                .Do(x => Raise.Event<ClientError>(this, new ClientError(1, 1, "Error")));

            await vm.ConnectCommand.ExecuteAsync();
            Assert.False(string.IsNullOrEmpty(vm.ErrorText));
            Assert.Equal("Connect", vm.ConnectButtonCaption);

            vm.ClearCommand.Execute(null);

            Assert.Equal(string.Empty, vm.ErrorText);            
        }

        [Theory]
        [InlineData(100000, 0.5, 1, 1, 500)]
        [InlineData(60000, 0.75, 1, 2, 900)]
        [InlineData(200000, 0.75, 2, 1, 3000)]
        public async Task RiskPerTradeCalculatedOnConnectionCorrectlyAsync(double netLiquidationValue, double exchangeRate, double riskMultiplier, double riskPercentOfAccountSize, double expected)
        {
            // Arrange
            var fired = false;

            var settingsRepository = Substitute.For<ISettingsRepository>();
            settingsRepository.GetAllAsync().Returns(new List<Setting>
                {
                    new Setting
                    {
                        Key = SettingsViewModel.SettingsKeys.RiskPercentOfAccountSize,
                        Value = riskPercentOfAccountSize.ToString()
                    },
                    new Setting
                    {
                        Key = SettingsViewModel.SettingsKeys.RiskMultiplier,
                        Value = riskMultiplier.ToString()
                    }
                });

            var vm = GetVm(settingsRepository);
            vm.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(MainViewModel.RiskPerTrade))
                {
                    fired = true;
                }
            };

            ConnectionTest(netLiquidationValue, exchangeRate);

            // Act
            await vm.ConnectCommand.ExecuteAsync();

            // Assert
            Assert.Equal(expected, vm.RiskPerTrade);
            Assert.True(fired);
        }

        [Fact]
        public async Task StatusShownCorrectlyWhenConnectedAsync()
        {
            // Arrange
            var vm = GetVm();
            ConnectionTest(10000, 0.5);

            // Act
            await vm.ConnectCommand.ExecuteAsync();

            // Assert
            Assert.Equal("Disconnect", vm.ConnectButtonCaption);
            Assert.True(vm.IsEnabled);
            Assert.Equal("Connected to TWS", _statusBarViewModel.ConnectionStatusText);
        }

        private void ConnectionTest(double netLiquidationValue, double exchangeRate)
        {
            _connectionService
                .When(x => x.ConnectAsync())
                .Do(x =>
                {                    
                    _connectionService.IsConnected.Returns(true);
                    Messenger.Default.Send(new ConnectionChangedMessage(true));
                });
            
            _accountManager.RequestAccountSummaryAsync().Returns(Task.FromResult(new AccountSummaryCompletedMessage
            {
                NetLiquidation = netLiquidationValue
            }));

            _exchangeRateService.GetExchangeRateAsync().Returns(Task.FromResult(exchangeRate));
        }

        [Fact]
        public async Task WhenAppClosingThenConnectionClosedAsync()
        {
            // Act
            var vm = GetVm();
            ConnectionTest(0, 0);
            await vm.ConnectCommand.ExecuteAsync();

            // Act
            vm.AppIsClosing();

            // Assert
            await _connectionService.Received().DisconnectAsync();
        }

        [Fact]
        public async Task WhenUpdatingSettingThenPersistImmediately()
        {
            // Arrange
            const double Multiplier = 1.234;

            var vm = GetVm();            
            var settings = new List<Setting>();
            _settingsRepository.GetAllAsync().Returns(settings);

            // HACK: Wait here to allow the viewmodel's background task to load the settings initially
            await Task.Delay(100);

            // Act
            vm.RiskMultiplier = Multiplier;

            // Assert
            Assert.Equal(Multiplier, _settingsViewModel.LastRiskMultiplier);
            _settingsRepository
                .Received()
                .Update(Arg.Is<Setting>(x => x.Key == SettingsViewModel.SettingsKeys.RiskMultiplier && x.Value == Multiplier.ToString()));
            await _settingsRepository.Received().SaveAsync();
        }

        [Fact]
        public async Task StartingAppLoadsSettingsFromRepository()
        {
            // Arrange
            GetVm();            

            // Assert
            await _settingsRepository.Received().GetAllAsync();
        }

        [Fact]
        public async Task WhenConnectionMadePositionsRequestedAsync()
        {
            // Arrange
            var vm = GetVm();
            ConnectionTest(0, 0);

            // Act
            await vm.ConnectCommand.ExecuteAsync();

            // Assert
            await _accountManager.Received().RequestPositionsAsync();
        }

        [Fact]
        public async Task WhenOrderIsFilledDeleteAndRequestPositionsAsync()
        {
            // Arrange
            const int OrderId = 123;

            var vm = GetVm();
            ConnectionTest(0, 0);
            await vm.ConnectCommand.ExecuteAsync();
            vm.OrdersViewModel.AddCommand.Execute(null);
            var order = vm.OrdersViewModel.Orders[0];
            order.Id = OrderId;

            // Act            
            Messenger.Default.Send(new OrderStatusChangedMessage(string.Empty, new OrderStatusEventArgs(OrderId, BrokerConstants.OrderStatus.Filled, 0, 0, 0, 0, 0, 0, 0, null)), OrderStatusChangedMessage.Tokens.Orders);

            // Assert
            await _accountManager.Received(2).RequestPositionsAsync(); // 2 requests - one initially and a second once filled
            Assert.Empty(vm.OrdersViewModel.Orders);
        }

        [Fact]
        public async Task ModifyingRiskPercentageOfAccountSizeModifiesRiskPerTradeAsync()
        {
            var settingsRepository = Substitute.For<ISettingsRepository>();
            settingsRepository.GetAllAsync().Returns(new List<Setting>
                {
                    new Setting
                    {
                        Key = SettingsViewModel.SettingsKeys.RiskPercentOfAccountSize,
                        Value = "0.1"                        
                    },
                    new Setting
                    {
                        Key = SettingsViewModel.SettingsKeys.RiskMultiplier,
                        Value = "1"
                    }
                });

            var vm = GetVm(settingsRepository);
            ConnectionTest(10000, 1);
            await vm.ConnectCommand.ExecuteAsync();
            Assert.Equal(10, vm.RiskPerTrade);

            _settingsViewModel.RiskPercentOfAccountSize = 2;
            Assert.Equal(200, vm.RiskPerTrade);
        }
    }
}
