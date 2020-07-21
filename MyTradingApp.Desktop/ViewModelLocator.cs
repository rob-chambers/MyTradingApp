using GalaSoft.MvvmLight;
using Microsoft.Extensions.DependencyInjection;
using MyTradingApp.ViewModels;

namespace MyTradingApp.Desktop
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
        }

        public MainViewModel Main => ServiceProviderFactory.ServiceProvider.GetService<MainViewModel>();

        public SettingsViewModel Settings => ServiceProviderFactory.ServiceProvider.GetService<SettingsViewModel>();

        public OrdersViewModel Orders => ServiceProviderFactory.ServiceProvider.GetService<OrdersViewModel>();

        public StatusBarViewModel StatusBar => ServiceProviderFactory.ServiceProvider.GetService<StatusBarViewModel>();

        public PositionsViewModel Positions => ServiceProviderFactory.ServiceProvider.GetService<PositionsViewModel>();

        public DetailsViewModel Details => ServiceProviderFactory.ServiceProvider.GetService<DetailsViewModel>();
    }
}
