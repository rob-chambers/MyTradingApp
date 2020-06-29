using GalaSoft.MvvmLight.Messaging;
using IBApi;
using MyTradingApp.EventMessages;
using MyTradingApp.Messages;
using MyTradingApp.Models;
using MyTradingApp.Services;
using MyTradingApp.ViewModels;
using NSubstitute;
using System;
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
        private OrdersViewModel _ordersViewModel;
        private StatusBarViewModel _statusBarViewModel;        

        private MainViewModel GetVm()
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
            var orderManager = Substitute.For<IOrderManager>();
            _ordersViewModel = new OrdersViewModel(_contractManager, _marketDataManager, _historicalDataManager, _orderCalculationService, orderManager);
            _statusBarViewModel = Substitute.For<StatusBarViewModel>();

            var positionsViewModel = new PositionsViewModel();

            return new MainViewModel(_ibClient, _connectionService, _orderManager, _accountManager, _ordersViewModel, _statusBarViewModel, _historicalDataManager, _exchangeRateService, _orderCalculationService, positionsViewModel);
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
                });

            _accountManager
                .When(x => x.RequestAccountSummary())
                .Do(x => Messenger.Default.Send(new AccountSummaryCompletedMessage
                {
                    NetLiquidation = netLiquidationValue
                }));

            _exchangeRateService
                .When(x => x.RequestExchangeRate())
                .Do(x => Messenger.Default.Send(new ExchangeRateMessage(exchangeRate)));
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
            _connectionService
                .When(x => x.Connect())
                .Do(x =>
                {
                    _connectionService.IsConnected.Returns(true);
                });

            // Act
            vm.ConnectCommand.Execute(null);

            // Assert
            _accountManager.Received().RequestPositions();
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
            Messenger.Default.Send(new OrderStatusChangedMessage(string.Empty, new OrderStatusMessage(OrderId, BrokerConstants.OrderStatus.Filled, 0, 0, 0, 0, 0, 0, 0, null, 0)));

            // Assert
            _accountManager.Received(2).RequestPositions(); // 2 requests - one initially and a second once filled
            Assert.Empty(vm.OrdersViewModel.Orders);
        }
    }
}
