using MyTradingApp.Core.ViewModels;

namespace MyTradingApp.Core.EventMessages
{
    public class OrderSelectionChangedMessage
    {
        public OrderSelectionChangedMessage(NewOrderViewModel order)
        {
            Order = order;
        }

        public NewOrderViewModel Order { get; }
    }
}
