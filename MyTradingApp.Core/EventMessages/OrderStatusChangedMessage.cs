using MyTradingApp.Messages;

namespace MyTradingApp.EventMessages
{
    public class OrderStatusChangedMessage : SymbolMessage
    {
        public OrderStatusChangedMessage(string symbol, OrderStatusMessage message)
            : base(symbol)
        {
            Message = message;
        }

        public OrderStatusMessage Message { get; }
    }
}