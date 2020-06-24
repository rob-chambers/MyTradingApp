using MyTradingApp.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MyTradingApp.Services
{
    internal class OrderCalculationService : IOrderCalculationService
    {
        /// <summary>
        /// Provide a minimum buffer price of 5 cents, when setting the stop loss below a prior low
        /// </summary>
        private const double MinBuffer = 0.05;

        private readonly Dictionary<string, ICollection<Bar>> _bars = new Dictionary<string, ICollection<Bar>>();
        private double _latestPrice;
        private double _riskPerTrade;

        public bool CanCalculate(string symbol)  => !double.IsNaN(_latestPrice) &&
            _latestPrice != 0 &&
            _bars != null &&
            _bars.ContainsKey(symbol);

        public double CalculateInitialStopLoss(string symbol)
        {
            //var ma = CalculateMovingAverage();
            var sd = CalculateStandardDeviation(symbol);

            var lowerBand = _latestPrice - sd;

            lowerBand = CheckToAdjustBelowRecentLow(symbol, lowerBand);

            return Math.Round(lowerBand, 2);
        }

        public double CalculateStandardDeviation(string symbol)
        {
            var values = new List<double>();
            var mean = CalculateMovingAverage(symbol);
            for (var i = 0; i < _bars[symbol].Count; i++)
            {
                var value = _bars[symbol].ElementAt(i).Close - mean;
                value *= value;
                values.Add(value);
            }

            var avg = values.Average();
            return Math.Sqrt(avg);
        }

        public double GetCalculatedQuantity(string symbol)
        {
            var diff = Math.Abs(GetEntryPrice(symbol) - CalculateInitialStopLoss(symbol));
            var size = _riskPerTrade / diff;

            return Math.Round(size, 0);
        }

        public double GetEntryPrice(string symbol)
        {
            // TODO: Calculate buffer based on volatility
            var buffer = 0.05D;
            if (_latestPrice >= 20)
            {
                buffer = 0.12;
            }

            return _latestPrice + buffer;
        }

        public void SetHistoricalData(string symbol, ICollection<Bar> bars)
        {
            if (_bars.ContainsKey(symbol))
            {
                _bars.Remove(symbol);
            }

            _bars.Add(symbol, bars);
        }

        public void SetLatestPrice(string symbol, double price)
        {
            _latestPrice = price;
        }

        public void SetRiskPerTrade(double value)
        {
            _riskPerTrade = value;
        }

        private double CalculateMovingAverage(string symbol)
        {
            return _bars[symbol].Average(x => x.Close);
        }

        private double CheckToAdjustBelowRecentLow(string symbol, double lowerBand)
        {
            var highestPrice = _bars[symbol].Max(x => x.Close);
            var ratio = (highestPrice - lowerBand) / 30;
            var maxDistanceAboveLow = Math.Round(ratio, 2);
            var buffer = maxDistanceAboveLow / 2;
            if (buffer < MinBuffer)
            {
                buffer = MinBuffer;
            }

            var result = lowerBand;
            for (var i = 0; i < _bars.Count; i++)
            {
                var low = _bars[symbol].ElementAt(i).Low;
                if (lowerBand >= low && lowerBand <= low + maxDistanceAboveLow)
                {
                    result = low - buffer;
                    break;
                }
            }

            return result;
        }
    }
}