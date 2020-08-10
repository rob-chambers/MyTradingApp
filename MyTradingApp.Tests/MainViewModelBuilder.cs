using MyTradingApp.Core;
using MyTradingApp.Core.Repositories;
using MyTradingApp.Core.Services;
using MyTradingApp.Core.Utils;
using MyTradingApp.Core.ViewModels;
using MyTradingApp.Domain;
using NSubstitute;
using System;
using System.Collections.Generic;

namespace MyTradingApp.Tests
{
    internal class MainViewModelBuilder
    {
        private IConnectionService _connectionService;
        private IAccountManager _accountManager;
        private IOrderCalculationService _orderCalculationService;
        private ISettingsRepository _settingsRepository;
        private IRiskCalculationService _riskCalculationService;

        public IConnectionService ConnectionService { get; private set; }

        public MainViewModelBuilder WithConnectionService(IConnectionService connectionService)
        {
            _connectionService = connectionService;
            return this;
        }

        public MainViewModelBuilder WithAccountManager(IAccountManager accountManager)
        {
            _accountManager = accountManager;
            return this;
        }

        public ISettingsRepository SettingsRepository { get; private set; }

        public MainViewModelBuilder WithRiskCalculationService(IRiskCalculationService riskCalculationService)
        {
            _riskCalculationService = riskCalculationService;
            return this;
        }

        public MainViewModelBuilder WithOrderCalculationService(IOrderCalculationService orderCalculationService)
        {
            _orderCalculationService = orderCalculationService;
            return this;
        }

        public MainViewModelBuilder WithSettingsRepository(ISettingsRepository settingsRepository)
        {
            _settingsRepository = settingsRepository;
            return this;
        }

        public SettingsViewModel SettingsViewModel { get; private set; }

        public MainViewModel Build()
        {
            ConnectionService = _connectionService ?? Substitute.For<IConnectionService>();
            var orderManager = Substitute.For<IOrderManager>();
            var orderCalculationService = _orderCalculationService ?? Substitute.For<IOrderCalculationService>();

            var marketDataManager = Substitute.For<IMarketDataManager>();
            var positionsManager = Substitute.For<IPositionManager>();
            var contractManager = Substitute.For<IContractManager>();

            var queueProcessor = Substitute.For<IQueueProcessor>();
            queueProcessor
                .When(x => x.Enqueue(Arg.Any<Action>()))
                .Do(x => x.Arg<Action>().Invoke());

            var dispatcherHelper = Substitute.For<IDispatcherHelper>();
            dispatcherHelper
                .When(x => x.InvokeOnUiThread(Arg.Any<Action>()))
                .Do(x => x.Arg<Action>().Invoke());

            var accountManager = _accountManager ?? Substitute.For<IAccountManager>();

            var positionsViewModel = new PositionsViewModel(dispatcherHelper, marketDataManager, accountManager, positionsManager, contractManager, queueProcessor);

            SettingsRepository = _settingsRepository;
            if (SettingsRepository == null)
            {
                SettingsRepository = Substitute.For<ISettingsRepository>();
                SettingsRepository.GetAllAsync().Returns(new List<Setting>
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

            SettingsViewModel = new SettingsViewModel(SettingsRepository);

            var findSymbolService = Substitute.For<IFindSymbolService>();
            var factory = new NewOrderViewModelFactory(dispatcherHelper, queueProcessor, findSymbolService, orderCalculationService, orderManager);
            var tradeRepository = Substitute.For<ITradeRepository>();
            var riskCalculationService = _riskCalculationService ?? Substitute.For<IRiskCalculationService>();

            MainViewModel.IsUnitTesting = true;
            return new MainViewModel(
                dispatcherHelper,
                ConnectionService,
                orderCalculationService,
                positionsViewModel,
                SettingsViewModel,
                new OrdersListViewModel(dispatcherHelper, queueProcessor, factory, tradeRepository, marketDataManager),
                riskCalculationService);
        }
    }
}
