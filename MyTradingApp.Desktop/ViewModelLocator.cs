using AutoFinance.Broker.InteractiveBrokers;
using AutoFinance.Broker.InteractiveBrokers.Controllers;
using GalaSoft.MvvmLight;
using IBApi;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MyTradingApp.Core;
using MyTradingApp.Core.Persistence;
using MyTradingApp.Core.Repositories;
using MyTradingApp.Core.Services;
using MyTradingApp.Core.Utils;
using MyTradingApp.Core.ViewModels;
using MyTradingApp.Desktop.Utils;
using MyTradingApp.Domain;
using Serilog;
using System.Windows;

namespace MyTradingApp.Desktop
{
    internal class ViewModelLocator
    {
        private readonly ServiceProvider _serviceProvider;

        /// <summary>
        /// Initializes a new instance of the ViewModelLocator class.
        /// </summary>
        public ViewModelLocator()
        {
            Log.Debug("In {0} ctor", nameof(ViewModelLocator));

            var isDesignTime = ViewModelBase.IsInDesignModeStatic;
            var serviceCollection = new ServiceCollection();
            ConfigureServices(serviceCollection, isDesignTime);

            _serviceProvider = serviceCollection.BuildServiceProvider();
        }

        public MainViewModel Main => _serviceProvider.GetService<MainViewModel>();

        public SettingsViewModel Settings => _serviceProvider.GetService<SettingsViewModel>();

        public OrdersListViewModel OrdersList => _serviceProvider.GetService<OrdersListViewModel>();

        public FindSymbolViewModel FindSymbol => _serviceProvider.GetService<FindSymbolViewModel>();

        public StatusBarViewModel StatusBar => _serviceProvider.GetService<StatusBarViewModel>();

        public PositionsViewModel Positions => _serviceProvider.GetService<PositionsViewModel>();

        public DetailsViewModel Details => _serviceProvider.GetService<DetailsViewModel>();

        private static void ConfigureServices(IServiceCollection services, bool isDesignTime)
        {
            Log.Debug("Configuring services");

            var app = (App)Application.Current;
            if (!isDesignTime)
            {                
                var connectionString = ConfigurationExtensions.GetConnectionString(app.Configuration, "DefaultConnection");
                services.AddDbContext<ApplicationContext>(options => options.UseSqlServer(connectionString));
                services.AddSingleton<IApplicationContext, ApplicationContext>();
            }

            services.AddScoped<EReaderSignal, EReaderMonitorSignal>();
            services.AddScoped<EReaderSignal, EReaderMonitorSignal>();
            services.AddScoped<MainViewModel>();
            services.AddScoped<OrdersListViewModel>();
            services.AddScoped<PositionsViewModel>();
            services.AddScoped<SettingsViewModel>();
            services.AddScoped<StatusBarViewModel>();
            services.AddScoped<DetailsViewModel>();
            services.AddScoped<FindSymbolViewModel>();
            services.AddScoped<IAccountManager, AccountManager>();
            services.AddScoped<IConnectionService, ConnectionService>();
            services.AddScoped<IOrderManager, OrderManager>();
            services.AddScoped<IContractManager, ContractManager>();
            services.AddScoped<IMarketDataManager, MarketDataManager>();
            services.AddScoped<IHistoricalDataManager, HistoricalDataManager>();
            services.AddScoped<IPositionManager, PositionManager>();
            services.AddScoped<IOrderCalculationService, OrderCalculationService>();
            services.AddScoped<IExchangeRateService, ExchangeRateService>();
            services.AddScoped<ITradeRepository, TradeRepository>();
            services.AddScoped<ISettingsRepository, SettingsRepository>();
            services.AddSingleton<ITwsObjectFactory>(new TwsObjectFactory("127.0.0.1", 7497, BrokerConstants.ClientId));
            services.AddScoped<IDispatcherHelper, DispatcherHelper>();
            services.AddScoped<IQueueProcessor, BlockingCollectionQueue>();
            services.AddScoped<IFindSymbolService, FindSymbolService>();
            services.AddScoped<INewOrderViewModelFactory, NewOrderViewModelFactory>();
            services.AddScoped(x => app.Configuration);
            services.AddScoped<IRiskCalculationService, RiskCalculationService>();
            services.AddScoped<ITradeRecordingService, TradeRecordingService>();
        }
    }
}
