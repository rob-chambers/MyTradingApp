using CommonServiceLocator;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Ioc;
using IBApi;
using MyTradingApp.Services;
using MyTradingApp.Views;

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
            SimpleIoc.Default.Register<SettingsViewModel>();
            SimpleIoc.Default.Register<IBClient>();
            SimpleIoc.Default.Register<IAccountManager, AccountManager>();
            SimpleIoc.Default.Register<IConnectionService, ConnectionService>();
            SimpleIoc.Default.Register<IOrderManager, OrderManager>();
            SimpleIoc.Default.Register<IContractManager, ContractManager>();            
        }

        public MainViewModel Main
        {
            get
            {
                return ServiceLocator.Current.GetInstance<MainViewModel>();
            }
        }

        public SettingsViewModel Settings
        {
            get
            {
                return ServiceLocator.Current.GetInstance<SettingsViewModel>(); ;
            }
        }

        public OrdersViewModel Orders
        {
            get
            {
                return ServiceLocator.Current.GetInstance<OrdersViewModel>();
            }
        }
    }
}
