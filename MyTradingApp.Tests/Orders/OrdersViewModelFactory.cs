using MyTradingApp.Services;
using MyTradingApp.ViewModels;
using NSubstitute;

namespace MyTradingApp.Tests.Orders
{
    internal class OrdersViewModelFactory
    {
        public IContractManager ContractManager { get; set; }

        public OrdersViewModel Create()
        {            
            return new OrdersViewModel(ContractManager);
        }
    }
}
