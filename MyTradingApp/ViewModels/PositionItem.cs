using GalaSoft.MvvmLight;
using IBApi;
using MyTradingApp.Models;
using MyTradingApp.TrailingStopStrategies;
using Serilog;
using System;

namespace MyTradingApp.ViewModels
{
    internal class PositionItem : ObservableObject
    {
        private Symbol _symbol;
        private double _avgPrice;
        private double _quantity;
        private double _profitLoss;
        private double _percentageGainLoss;
        private double _lastTrailingStopPercentage = 0;

        public PositionItem()
        {
            TrailingStopStrategy = new DayTradingProfitLockerStrategy();
        }

        public Symbol Symbol
        {
            get => _symbol;
            set => Set(ref _symbol, value);
        }

        public double AvgPrice
        {
            get => _avgPrice;
            set => Set(ref _avgPrice, value);
        }

        public double Quantity
        {
            get => _quantity;
            set => Set(ref _quantity, value);
        }

        public double ProfitLoss
        {
            get => _profitLoss;
            set => Set(ref _profitLoss, value);
        }

        public double PercentageGainLoss
        {
            get => _percentageGainLoss;
            set => Set(ref _percentageGainLoss, value);
        }

        public Contract Contract { get; set; }

        public Order Order { get; set; }

        public TrailingStopStrategy TrailingStopStrategy { get; set; }
        
        public ContractDetails ContractDetails { get; set; }

        public double? CheckToAdjustStop()
        {
            var stopPercentage = TrailingStopStrategy.CalculateTrailingStopPercentage(PercentageGainLoss);

            stopPercentage = Math.Round(stopPercentage, 1);

            var diff = stopPercentage - _lastTrailingStopPercentage;

            // TODO: Calculate appropriate threshold to move stop
            if (Math.Abs(diff) > 0.5)
            {
                Log.Debug("Found new stop price.  Old stop: {0}, new stop: {1}", _lastTrailingStopPercentage, stopPercentage);
                _lastTrailingStopPercentage = stopPercentage;
                return stopPercentage;
            }

            return null;
        }
    }
}
