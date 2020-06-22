using IBApi;
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
        private OrdersViewModel _ordersViewModel;
        private StatusBarViewModel _statusBarViewModel;

        private MainViewModel GetVm()
        {
            _ibClient = new IBClient(new EReaderMonitorSignal());
            _connectionService = Substitute.For<IConnectionService>();
            _orderManager = Substitute.For<IOrderManager>();
            _accountManager = Substitute.For<IAccountManager>();
            _contractManager = Substitute.For<IContractManager>();
            _marketDataManager = Substitute.For<IMarketDataManager>();
            _historicalDataManager = Substitute.For<IHistoricalDataManager>();
            _orderCalculationService = Substitute.For<IOrderCalculationService>();
            var orderManager = Substitute.For<IOrderManager>();
            _ordersViewModel = new OrdersViewModel(_contractManager, _marketDataManager, _historicalDataManager, _orderCalculationService, orderManager);
            _statusBarViewModel = Substitute.For<StatusBarViewModel>();

            return new MainViewModel(_ibClient, _connectionService, _orderManager, _accountManager, _ordersViewModel, _statusBarViewModel, _historicalDataManager);
        }

        [Fact]
        public void ConnectionStatusInitiallyCorrect()
        {
            var vm = GetVm();
            Assert.Equal("Connect", vm.ConnectButtonCaption);
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
    }
}
