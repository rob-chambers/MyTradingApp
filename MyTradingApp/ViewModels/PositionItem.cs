using GalaSoft.MvvmLight;
using IBApi;
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
            set
            {
                Set(ref _quantity, value);
                RaisePropertyChanged(nameof(IsOpen));
            }
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

        public bool IsOpen => Quantity != 0;

        public Contract Contract { get; set; }

        public Order Order { get; set; }

        public TrailingStopStrategy TrailingStopStrategy { get; set; }
        
        public ContractDetails ContractDetails { get; set; }

        public double? CheckToAdjustStop()
        {
            var stopPercentage = TrailingStopStrategy.CalculateTrailingStopPercentage(PercentageGainLoss);

            stopPercentage = Math.Round(stopPercentage, 1);

            var diff = _lastTrailingStopPercentage - stopPercentage;

            // TODO: Calculate appropriate threshold to move stop
            const double Buffer = 0.5;

            if (diff > Buffer || _lastTrailingStopPercentage == 0)
            {
                Log.Debug("Found new stop price.  Old stop: {0}, new stop: {1}", _lastTrailingStopPercentage, stopPercentage);
                _lastTrailingStopPercentage = stopPercentage;
                return stopPercentage;
            }

            return null;
        }
    }
}
