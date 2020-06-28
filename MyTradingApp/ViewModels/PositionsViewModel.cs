using GalaSoft.MvvmLight;
using MyTradingApp.Models;
using System.Collections.ObjectModel;

namespace MyTradingApp.ViewModels
{
    internal class PositionsViewModel : ViewModelBase
    {
        public ObservableCollection<PositionItem> Positions { get; } = new ObservableCollection<PositionItem>();

        public PositionsViewModel()
        {
            Positions.Add(new PositionItem
            {
                AvgPrice = 11.03,
                ProfitLoss = 231.56,
                Quantity = 233,
                Symbol = new Symbol
                {
                    Code = "CAT",
                    Name = "Caterpillar",
                    LatestPrice = 11.87,
                }
            });
        }
    }

    internal class PositionItem : ViewModelBase
    {
        private Symbol _symbol;
        private double _avgPrice;
        private double _quantity;
        private double _profitLoss;

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
    }
}
