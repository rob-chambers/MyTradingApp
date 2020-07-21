using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Ioc;
using IBApi;
using MyTradingApp.Persistence;
using MyTradingApp.Repositories;
using MyTradingApp.Services;
using MyTradingApp.ViewModels;

namespace MyTradingApp
{
    internal class ViewModelLocator
    {
        /// <summary>
        /// Initializes a new instance of the ViewModelLocator class.
        /// </summary>
        public ViewModelLocator()
        {
            if (ViewModelBase.IsInDesignModeStatic)
            {
                // Create design time view services and models
            }
            else
            {
                // Create run time view services and models
            }

            
            SimpleIoc.Default.Register<EReaderSignal, EReaderMonitorSignal>();
            SimpleIoc.Default.Register<MainViewModel>();
            SimpleIoc.Default.Register<OrdersViewModel>();
            SimpleIoc.Default.Register<PositionsViewModel>();
            SimpleIoc.Default.Register<SettingsViewModel>();
            SimpleIoc.Default.Register<StatusBarViewModel>();
            SimpleIoc.Default.Register<DetailsViewModel>();
            SimpleIoc.Default.Register<IBClient>();
            SimpleIoc.Default.Register<IAccountManager, AccountManager>();
            SimpleIoc.Default.Register<IConnectionService, ConnectionService>();
            SimpleIoc.Default.Register<IOrderManager, OrderManager>();
            SimpleIoc.Default.Register<IContractManager, ContractManager>();
            SimpleIoc.Default.Register<IMarketDataManager, MarketDataManager>();
            SimpleIoc.Default.Register<IHistoricalDataManager, HistoricalDataManager>();
            SimpleIoc.Default.Register<IPositionManager, PositionManager>();
            SimpleIoc.Default.Register<IOrderCalculationService, OrderCalculationService>();
            SimpleIoc.Default.Register<IExchangeRateService, ExchangeRateService>();
            SimpleIoc.Default.Register<ITradeRepository, TradeRepository>();
            SimpleIoc.Default.Register<IApplicationContext, ApplicationContext>();
        }

        public MainViewModel Main => SimpleIoc.Default.GetInstance<MainViewModel>();

        public SettingsViewModel Settings => SimpleIoc.Default.GetInstance<SettingsViewModel>();

        public OrdersViewModel Orders => SimpleIoc.Default.GetInstance<OrdersViewModel>();

        public StatusBarViewModel StatusBar => SimpleIoc.Default.GetInstance<StatusBarViewModel>();

        public PositionsViewModel Positions => SimpleIoc.Default.GetInstance<PositionsViewModel>();

        public DetailsViewModel Details => SimpleIoc.Default.GetInstance<DetailsViewModel>();
    }
}
