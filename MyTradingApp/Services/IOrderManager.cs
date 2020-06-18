using IBApi;
using MyTradingApp.Messages;
using System.Collections.Generic;

namespace MyTradingApp.Services
{
    internal interface IOrderManager
    {
        List<string> ManagedAccounts { get; set; }

        void PlaceOrder(Contract contract, Order order);
        void HandleOrderStatus(OrderStatusMessage message);
    }
}