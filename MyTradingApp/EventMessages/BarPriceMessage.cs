using MyTradingApp.Domain;

namespace MyTradingApp.EventMessages
{
    public class BarPriceMessage : SymbolMessage
    {
        public BarPriceMessage(string symbol, Bar bar)
            : base(symbol)
        {
            Symbol = symbol;
            Bar = bar;
        }

        public Bar Bar { get; }
    }
}