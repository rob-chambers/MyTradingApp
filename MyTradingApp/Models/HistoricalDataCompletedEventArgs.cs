using System;
using System.Collections.Generic;

namespace MyTradingApp.Models
{
    public  class HistoricalDataCompletedEventArgs : EventArgs
    {
        public HistoricalDataCompletedEventArgs(ICollection<Bar> bars)
        {
            Bars = bars;
        }

        public ICollection<Bar> Bars { get; }
    }
}
