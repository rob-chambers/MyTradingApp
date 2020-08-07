using MyTradingApp.Core.ViewModels;

namespace MyTradingApp.Core.EventMessages
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
