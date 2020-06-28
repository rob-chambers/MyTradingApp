using CommonServiceLocator;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Ioc;
using IBApi;
using MyTradingApp.Services;

namespace MyTradingApp.ViewModels
{
    internal class ViewModelLocator
    {
        /// <summary>
        /// Initializes a new instance of the ViewModelLocator class.
        /// </summary>
        public ViewModelLocator()
        {
            ServiceLocator.SetLocatorProvider(() => SimpleIoc.Default);

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
            SimpleIoc.Default.Register<IBClient>();
            SimpleIoc.Default.Register<IAccountManager, AccountManager>();
            SimpleIoc.Default.Register<IConnectionService, ConnectionService>();
            SimpleIoc.Default.Register<IOrderManager, OrderManager>();
            SimpleIoc.Default.Register<IContractManager, ContractManager>();
            SimpleIoc.Default.Register<IMarketDataManager, MarketDataManager>();
            SimpleIoc.Default.Register<IHistoricalDataManager, HistoricalDataManager>();
            SimpleIoc.Default.Register<IOrderCalculationService, OrderCalculationService>();
            SimpleIoc.Default.Register<IExchangeRateService, ExchangeRateService>();
        }        

        public MainViewModel Main => ServiceLocator.Current.GetInstance<MainViewModel>();

        public SettingsViewModel Settings => ServiceLocator.Current.GetInstance<SettingsViewModel>();

        public OrdersViewModel Orders => ServiceLocator.Current.GetInstance<OrdersViewModel>();

        public StatusBarViewModel StatusBar => ServiceLocator.Current.GetInstance<StatusBarViewModel>();

        public PositionsViewModel Positions => ServiceLocator.Current.GetInstance<PositionsViewModel>();
    }
}
