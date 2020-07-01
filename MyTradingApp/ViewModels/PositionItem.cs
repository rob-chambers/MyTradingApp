using GalaSoft.MvvmLight;
using IBApi;
using MyTradingApp.Models;

namespace MyTradingApp.ViewModels
{
    internal class PositionItem : ObservableObject
    {
        private Symbol _symbol;
        private double _avgPrice;
        private double _quantity;
        private double _profitLoss;
        private double _percentageGainLoss;

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
    }
}
