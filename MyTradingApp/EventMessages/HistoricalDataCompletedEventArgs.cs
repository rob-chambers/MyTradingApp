using MyTradingApp.EventMessages;
using System.Collections.Generic;

namespace MyTradingApp.Models
{
    public class HistoricalDataCompletedMessage : SymbolMessage
    {
        public HistoricalDataCompletedMessage(string symbol, ICollection<Bar> bars)
            : base(symbol)
        {
            Bars = bars;
        }

        public ICollection<Bar> Bars { get; }
    }
}