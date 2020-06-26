using MyTradingApp.Models;

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
