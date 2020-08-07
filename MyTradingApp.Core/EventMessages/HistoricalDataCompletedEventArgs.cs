using MyTradingApp.Domain;

namespace MyTradingApp.Core.EventMessages
{
    public class HistoricalDataCompletedMessage : SymbolMessage
    {
        public HistoricalDataCompletedMessage(string symbol, BarCollection bars)
            : base(symbol)
        {
            Bars = bars;
        }

        public BarCollection Bars { get; }
    }
}