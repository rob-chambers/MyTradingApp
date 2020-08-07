using MyTradingApp.Domain;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MyTradingApp.Core.Services
{
    public class OrderCalculationService : IOrderCalculationService
    {
        /// <summary>
        /// Provide a minimum buffer price of 5 cents, when setting the stop loss below a prior low
        /// </summary>
        private const double MinBuffer = 0.05;

        private readonly Dictionary<string, BarCollection> _bars = new Dictionary<string, BarCollection>();
        private readonly Dictionary<string, double> _latestPrice = new Dictionary<string, double>();
        private double _riskPerTrade;

        public bool CanCalculate(string symbol) => _latestPrice.ContainsKey(symbol) &&
            !double.IsNaN(_latestPrice[symbol]) &&
            _latestPrice[symbol] != 0 &&
            _bars != null &&
            _bars.ContainsKey(symbol);

        public double CalculateInitialStopLoss(string symbol, Direction direction)
        {
            //var ma = CalculateMovingAverage();
            var sd = CalculateStandardDeviation(symbol);

            double band = 0;
            if (direction == Direction.Buy)
            {
                band = _latestPrice[symbol] - sd;
                band = CheckToAdjustBelowRecentLow(symbol, band);
            }
            else if (direction == Direction.Sell)
            {
                band = _latestPrice[symbol] + sd;

                // TODO: Check for price near a recent high
            }

            return Math.Round(band, 2);
        }

        public double CalculateStandardDeviation(string symbol)
        {
            var values = new List<double>();
            var mean = CalculateMovingAverage(symbol);
            for (var i = 0; i < _bars[symbol].Count; i++)
            {
                var value = _bars[symbol].ElementAt(i).Value.Close - mean;
                value *= value;
                values.Add(value);
            }

            var avg = values.Average();
            return Math.Sqrt(avg);
        }

        public ushort GetCalculatedQuantity(string symbol, Direction direction)
        {
            var diff = Math.Abs(GetEntryPrice(symbol, direction) - CalculateInitialStopLoss(symbol, direction));
            var size = _riskPerTrade / diff;

            var roundedSize = Math.Round(size, 0);
            if (roundedSize > ushort.MaxValue)
            {
                roundedSize = ushort.MaxValue;
            }

            return Convert.ToUInt16(roundedSize);
        }

        public double GetEntryPrice(string symbol, Direction direction)
        {
            // TODO: Calculate buffer based on volatility
            var buffer = 0.05D;
            if (_latestPrice[symbol] >= 20)
            {
                buffer = 0.12;
            }

            var price = direction == Direction.Buy
                ? _latestPrice[symbol] + buffer
                : _latestPrice[symbol] - buffer;

            return Math.Round(price, 2);
        }

        public void SetHistoricalData(string symbol, BarCollection bars)
        {
            if (_bars.ContainsKey(symbol))
            {
                _bars.Remove(symbol);
            }

            _bars.Add(symbol, bars);
        }

        public void SetLatestPrice(string symbol, double price)
        {
            if (_latestPrice.ContainsKey(symbol))
            {
                _latestPrice.Remove(symbol);
            }

            _latestPrice.Add(symbol, price);
        }

        public void SetRiskPerTrade(double value)
        {
            _riskPerTrade = value;
        }

        private double CalculateMovingAverage(string symbol)
        {
            return _bars[symbol].Average(x => x.Value.Close);
        }

        private double CheckToAdjustBelowRecentLow(string symbol, double lowerBand)
        {
            var highestPrice = _bars[symbol].Max(x => x.Value.Close);
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
                var low = _bars[symbol].ElementAt(i).Value.Low;
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