using System.Collections.Generic;

namespace MyTradingApp.Models
{
    public class HistoricalDataCompletedMessage
    {
        public HistoricalDataCompletedMessage(ICollection<Bar> bars)
        {
            Bars = bars;
        }

        public ICollection<Bar> Bars { get; }
    }
}