using MyTradingApp.Stops.StopTypes;
using System;
using System.Linq;

namespace MyTradingApp.Stops
{
    public class StopManager
    {
        public Position Position { get; set; }

        private double _standardStopPrice;

        public void AddLatestBar(Bar bar)
        {
            Bars.Add(bar.Date, bar);
        }

        public void SetHistoricalBars(BarCollection bars)
        {
            Bars = bars;
            foreach (var stop in Position.ExitStrategy.Stops)
            {
                stop.Reset();
            }
        }

        public BarCollection Bars { get; private set; }

        //public Stop GetStop()
        //{
        //    var latestBar = _bars.ElementAt(_bars.Count - 1);

        //}

        public Stop GetStop(DateTime date)
        {
            if (!Bars.ContainsKey(date))
            {
                return null;
            }

            var high = GetHighestHigh(date);
            var low = GetLowestLow(date);

            var currentPrice = Bars[date].Close;
            //var gain = (currentPrice - Position.EntryPrice) / Position.EntryPrice * 100;
            var gain = (high - Position.EntryPrice) / Position.EntryPrice * 100;
            if (Position.Direction == TradeDirection.Short)
            {
                gain = -gain;
            }

            //var exit = Position.ExitStrategy.GetExitForPercentageGain(gain);
            var stop = Position.ExitStrategy.GetStopForPercentageGain(gain);
            //var stop = exit.Stop;

            stop.CalculatePrice(Position, gain, high, low);

            //switch (stop.Type)
            //{
            //    case StopType.Trailing:
            //        var trail = (TrailingStop)stop;

            //        if (Position.Direction == TradeDirection.Long)
            //        {
            //            stop.Price = high - high * trail.Percentage / 100D;
            //        }
            //        else
            //        {
            //            stop.Price = low + low * trail.Percentage / 100D;
            //        }

            //        break;

            //    case StopType.Standard:
            //        if (_standardStopPrice != 0)
            //        {
            //            stop.Price = _standardStopPrice;
            //        }
            //        else
            //        {
            //            if (Position.Direction == TradeDirection.Long)
            //            {
            //                stop.Price = high - high * exit.LowerPercentage.Value / 100D;
            //            }
            //            else
            //            {
            //                stop.Price = low + low * exit.LowerPercentage.Value / 100D;
            //            }

            //            _standardStopPrice = stop.Price;
            //        }

            //        break;

            //    case StopType.Closing:
            //        var closingStop = (ClosingStop)stop;
            //        var trailPercentage = CalcClosingStopValue(gain, closingStop);

            //        if (Position.Direction == TradeDirection.Long)
            //        {
            //            stop.Price = high - high * trailPercentage / 100D;
            //        }
            //        else
            //        {
            //            stop.Price = low + low * trailPercentage / 100D;
            //        }

            //        break;
            //}

            return stop;
        }

        //private double CalcClosingStopValue(double gain, ClosingStop closingStop)
        //{
        //    var multiplier = (closingStop.Lower.TrailingStopPercentage - closingStop.Upper.TrailingStopPercentage) / (closingStop.Upper.GainPercentage - closingStop.Lower.GainPercentage);
        //    var value = closingStop.Lower.TrailingStopPercentage - (gain - closingStop.Lower.GainPercentage) * multiplier;

        //    return value;
        //}

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