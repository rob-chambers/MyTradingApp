using GalaSoft.MvvmLight.Messaging;
using MyTradingApp.EventMessages;
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
        private ICollection<Bar> _bars;
        private double _accountSize;

        public OrderCalculationService()
        {
            Messenger.Default.Register<AccountSummaryMessage>(this, HandleAccountSummaryMessage);
        }

        private void HandleAccountSummaryMessage(AccountSummaryMessage args)
        {
            _accountSize = args.AvailableFunds;
        }

        public void SetHistoricalData(ICollection<Bar> bars)
        {
            _bars = bars;
        }

        public double CalculateStandardDeviation()
        {
            var values = new List<double>();
            var mean = CalculateMovingAverage();
            for (var i = 0; i < _bars.Count; i++)
            {
                var value = _bars.ElementAt(i).Close - mean;
                value *= value;
                values.Add(value);
            }

            var avg = values.Average();
            return Math.Sqrt(avg);
        }

        public double CalculateInitialStopLoss()
        {
            var ma = CalculateMovingAverage();
            var sd = CalculateStandardDeviation();

            var lowerBand = _bars.ElementAt(0).Close - sd;

            lowerBand = CheckToAdjustBelowRecentLow(lowerBand);

            return Math.Round(lowerBand, 2);
        }

        private double CheckToAdjustBelowRecentLow(double lowerBand)
        {
            var highestPrice = _bars.Max(x => x.Close);
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
                var low = _bars.ElementAt(i).Low;
                if (lowerBand >= low && lowerBand <= low + maxDistanceAboveLow)
                {
                    result = low - buffer;
                    break;
                }
            }

            return result;
        }

        private double CalculateMovingAverage()
        {
            return _bars.Average(x => x.Close);
        }

        public double GetCalculatedQuantity()
        {
            var risk = _accountSize * 0.01;
            var price = 10;

            return Math.Round(risk / price, 0);
        }
    }
}
