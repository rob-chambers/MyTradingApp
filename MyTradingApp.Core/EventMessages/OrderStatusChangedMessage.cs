using AutoFinance.Broker.InteractiveBrokers.EventArgs;

namespace MyTradingApp.Core.EventMessages
{
    public class OrderStatusChangedMessage : SymbolMessage
    {
        public static class Tokens
        {
            public const string Positions = "Positions";
            public const string Orders = "Orders";
            public const string Main = "Main";
        }

        public OrderStatusChangedMessage(string symbol, OrderStatusEventArgs message)
            : base(symbol)
        {
            Message = message;
        }

        public OrderStatusEventArgs Message { get; }
    }
}