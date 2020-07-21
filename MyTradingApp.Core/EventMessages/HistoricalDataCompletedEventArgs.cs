using MyTradingApp.Domain;
using MyTradingApp.EventMessages;

namespace MyTradingApp.Models
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