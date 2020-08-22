using System;

namespace MyTradingApp.Core.ViewModels
{
    public class PositionSizerViewModel : MenuItemViewModel
    {
        private double _entryPrice;
        private double _stopLoss;
        private double _riskAmount = 1000;

        public double RiskAmount
        {
            get => _riskAmount;
            set
            {
                Set(ref _riskAmount, value);
                RaisePropertyChanged(nameof(Size));
            }
        }

        public double EntryPrice
        {
            get => _entryPrice;
            set
            {
                Set(ref _entryPrice, value);
                RaisePropertyChanged(nameof(Size));
            }
        }

        public double StopLoss
        {
            get => _stopLoss;
            set
            {
                Set(ref _stopLoss, value);
                RaisePropertyChanged(nameof(Size));
            }
        }

        public double Size
        {
            get
            {
                var diff = EntryPrice - StopLoss;
                return diff == 0
                    ? 0
                    : Math.Round(Math.Abs(RiskAmount / diff));
            }
        }
    }
}