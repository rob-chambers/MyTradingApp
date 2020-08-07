using MyTradingApp.Core.ViewModels;
using MyTradingApp.Domain;

namespace MyTradingApp.Tests.Orders
{
    internal class OrderBuilder
    {
        public OrderBuilder Default
        {
            get
            {
                Order = new OrderItem
                {
                    Direction = Direction.Buy,
                    EntryPrice = 10,
                    InitialStopLossPrice = 9,
                    Quantity = 100,
                    Symbol = new Symbol
                    {
                        Exchange = Exchange.NYSE
                    }
                };

                return this;
            }
        }

        public OrderItem Order { get; private set; }

        public OrderBuilder SetSymbol(string symbol)
        {
            Order.Symbol.Code = symbol;
            return this;
        }
    }
}
