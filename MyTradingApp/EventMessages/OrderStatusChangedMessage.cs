using MyTradingApp.Messages;

namespace MyTradingApp.EventMessages
{
    public class OrderStatusChangedMessage
    {
        public OrderStatusChangedMessage(OrderStatusMessage message)
        {
            Message = message;
        }

        public OrderStatusMessage Message { get; }
    }
}
