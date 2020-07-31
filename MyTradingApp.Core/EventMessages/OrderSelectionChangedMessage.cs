using MyTradingApp.ViewModels;

namespace MyTradingApp.EventMessages
{
    public class OrderSelectionChangedMessage
    {
        public OrderSelectionChangedMessage(OrderItem order)
        {
            Order = order;
        }

        public OrderItem Order { get; }
    }
}
