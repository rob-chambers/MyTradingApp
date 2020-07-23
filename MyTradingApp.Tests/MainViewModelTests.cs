using AutoFinance.Broker.InteractiveBrokers.EventArgs;
using GalaSoft.MvvmLight.Messaging;
using IBApi;
using MyTradingApp.Core.Repositories;
using MyTradingApp.Domain;
using MyTradingApp.EventMessages;
using MyTradingApp.Messages;
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
        private IBClient _ibClient;
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
            _ibClient = new IBClient(new EReaderMonitorSignal());
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
                _settingsRepository.GetAll().Returns(new List<Setting>
                {
                    new Setting
                    {
                        Key = "RiskPercentOfAccountSize",
                        Value = "1"
                    },
                    new Setting
                    {
                        Key = "LastRiskMultiplier",
                        Value = "1"
                    }
                });
            }
            else
            {
                _settingsRepository = settingsRepository;
            }

            var orderManager = Substitute.For<IOrderManager>();

            _ordersViewModel = new OrdersViewModel(_contractManager, _marketDataManager, _historicalDataManager, _orderCalculationService, orderManager, _tradeRepository);
            _statusBarViewModel = Substitute.For<StatusBarViewModel>();

            var positionsManager = Substitute.For<IPositionManager>();
            var contractManager = Substitute.For<IContractManager>();
            var positionsViewModel = new PositionsViewModel(_marketDataManager, _accountManager, positionsManager, contractManager);
            var detailsViewModel = new DetailsViewModel();
            _settingsViewModel = new SettingsViewModel(_settingsRepository);

            return new MainViewModel(_ibClient, _connectionService, _orderManager, _accountManager, _ordersViewModel, _statusBarViewModel, _exchangeRateService, _orderCalculationService, positionsViewModel, detailsViewModel, _settingsViewModel);
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
        public void WhenConnectionErrorThenShowErrorInTextBox()
        {
            var vm = GetVm();
            _connectionService
                .When(x => x.Connect())
                .Do(x => Raise.Event<ClientError>(this, new ClientError(1, 1, "Error", new OutOfMemoryException())));

            vm.ConnectCommand.Execute(null);
            Assert.False(string.IsNullOrEmpty(vm.ErrorText));
            Assert.Equal("Connect", vm.ConnectButtonCaption);

            vm.ClearCommand.Execute(null);

            Assert.Equal(string.Empty, vm.ErrorText);            
        }

        [Theory]
        [InlineData(100000, 0.5, 1, 500)]
        [InlineData(60000, 0.75, 1, 450)]
        [InlineData(200000, 0.75, 2, 3000)]
        public void RiskPerTradeCalculatedOnConnectionCorrectly(double netLiquidationValue, double exchangeRate, double riskMultiplier, double expected)
        {
            // Arrange
            var fired = false;            

            var vm = GetVm();
            vm.RiskMultiplier = riskMultiplier;
            vm.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(MainViewModel.RiskPerTrade))
                {
                    fired = true;
                }
            };

            ConnectionTest(netLiquidationValue, exchangeRate);

            // Act
            vm.ConnectCommand.Execute(null);

            // Assert
            Assert.Equal(expected, vm.RiskPerTrade);
            Assert.True(fired);
        }

        [Fact]
        public void StatusShownCorrectlyWhenConnected()
        {
            // Arrange
            var vm = GetVm();
            ConnectionTest(10000, 0.5);

            // Act
            vm.ConnectCommand.Execute(null);

            // Assert
            Assert.Equal("Disconnect", vm.ConnectButtonCaption);
            Assert.True(vm.IsEnabled);
            Assert.Equal("Connected to TWS", _statusBarViewModel.ConnectionStatusText);
        }

        private void ConnectionTest(double netLiquidationValue, double exchangeRate)
        {
            _connectionService
                .When(x => x.Connect())
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
        public void WhenAppClosingThenConnectionClosed()
        {
            // Act
            var vm = GetVm();
            ConnectionTest(0, 0);
            vm.ConnectCommand.Execute(null);

            // Act
            vm.AppIsClosing();

            // Assert
            _connectionService.Received().Disconnect();
        }

        [Fact]
        public void WhenConnectionMadePositionsRequested()
        {
            // Arrange
            var vm = GetVm();
            ConnectionTest(0, 0);

            // Act
            vm.ConnectCommand.Execute(null);

            // Assert
            _accountManager.Received().RequestPositionsAsync();
        }

        [Fact]
        public void WhenOrderIsFilledDeleteAndRequestPositions()
        {
            // Arrange
            const int OrderId = 123;

            var vm = GetVm();
            ConnectionTest(0, 0);
            vm.ConnectCommand.Execute(null);
            vm.OrdersViewModel.AddCommand.Execute(null);
            var order = vm.OrdersViewModel.Orders[0];
            order.Id = OrderId;

            // Act            
            Messenger.Default.Send(new OrderStatusChangedMessage(string.Empty, new OrderStatusEventArgs(OrderId, BrokerConstants.OrderStatus.Filled, 0, 0, 0, 0, 0, 0, 0, null)));

            // Assert
            _accountManager.Received(2).RequestPositionsAsync(); // 2 requests - one initially and a second once filled
            Assert.Empty(vm.OrdersViewModel.Orders);
        }

        [Fact]
        public void ModifyingRiskPercentageOfAccountSizeModifiesRiskPerTrade()
        {
            var settingsRepository = Substitute.For<ISettingsRepository>();
            settingsRepository.GetAll().Returns(new List<Setting>
                {
                    new Setting
                    {
                        Key = "RiskPercentOfAccountSize",
                        Value = "0.1"                        
                    },
                    new Setting
                    {
                        Key = "LastRiskMultiplier",
                        Value = "1"
                    }
                });

            var vm = GetVm(settingsRepository);
            ConnectionTest(10000, 1);
            vm.ConnectCommand.Execute(null);
            Assert.Equal(10, vm.RiskPerTrade);

            _settingsViewModel.RiskPercentOfAccountSize = 2;
            Assert.Equal(200, vm.RiskPerTrade);
        }
    }
}
