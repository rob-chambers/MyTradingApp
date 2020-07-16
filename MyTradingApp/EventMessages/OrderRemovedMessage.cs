using MyTradingApp.Models;
using MyTradingApp.ViewModels;

namespace MyTradingApp.EventMessages
{
    internal class OrderRemovedMessage
    {
        public OrderRemovedMessage(OrderItem order)
        {
            Order = order;
        }

        public OrderItem Order { get; }
    }
}
