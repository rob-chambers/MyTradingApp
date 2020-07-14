using MyTradingApp.Stops.StopTypes;
using System;
using System.Linq;

namespace MyTradingApp.Stops
{
    public class StopManager
    {
        private BarCollection _bars = new BarCollection();
        private double _high;
        private double _low;

        public Position Position { get; set; }

        public BarCollection Bars 
        {
            get => _bars;
            private set
            {
                _bars = value;

                // Performance enhancement - don't bother getting low if we are going long as stops are based on the high only
                if (Position.Direction == TradeDirection.Long)
                {
                    _high = GetHighestHigh(DateTime.MaxValue);
                }
                else
                {
                    _low = GetLowestLow(DateTime.MaxValue);
                }                                
            }
        }
        
        public void AddLatestBar(Bar bar)
        {
            Bars.Add(bar.Date, bar);
        }

        public void SetHistoricalBars(BarCollection bars)
        {
            if (Position == null)
            {
                throw new InvalidOperationException("No position set.");
            }

            Bars = bars;
            foreach (var stop in Position.ExitStrategy.Stops)
            {
                stop.Reset();
            }
        }

        public Stop GetStop(DateTime date)
        {
            if (Position == null)
            {
                throw new InvalidOperationException("No position set.");
            }

            if (!Bars.ContainsKey(date))
            {
                return null;
            }

            //var currentPrice = Bars[date].Close;
            //var gain = (currentPrice - Position.EntryPrice) / Position.EntryPrice * 100;
            double gain;
            if (Position.Direction == TradeDirection.Long)
            {
                gain = (_high - Position.EntryPrice) / Position.EntryPrice * 100; ;
            }
            else
            {
                gain = (Position.EntryPrice - _low) / Position.EntryPrice * 100;
            }

            var stop = Position.ExitStrategy.GetStopForPercentageGain(gain);
            stop.CalculatePrice(Position, gain, _high, _low);

            return stop;
        }

        private double GetHighestHigh(DateTime date)
        {
            var dates = Bars.Keys.Where(x => x <= date);
            double high = 0;
            foreach (var d in dates)
            {
                var bar = Bars[d];
                if (bar.High > high)
                {
                    high = bar.High;
                }
            }

            return high;
        }

        private double GetLowestLow(DateTime date)
        {
            var dates = Bars.Keys.Where(x => x <= date);
            var low = double.MaxValue;
            foreach (var d in dates)
            {
                var bar = Bars[d];
                if (bar.Low < low)
                {
                    low = bar.Low;
                }
            }

            return low;
        }
    }
}