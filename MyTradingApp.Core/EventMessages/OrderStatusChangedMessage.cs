using AutoFinance.Broker.InteractiveBrokers.EventArgs;

namespace MyTradingApp.EventMessages
{
    public class OrderStatusChangedMessage : SymbolMessage
    {
        public OrderStatusChangedMessage(string symbol, OrderStatusEventArgs message)
            : base(symbol)
        {
            Message = message;
        }

        public OrderStatusEventArgs Message { get; }
    }
}