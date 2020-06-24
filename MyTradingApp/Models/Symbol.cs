using GalaSoft.MvvmLight;

namespace MyTradingApp.Models
{
    internal class Symbol : ViewModelBase
    {
        private string _code;
        private Exchange _exchange;
        private string _name;
        private double _latestPrice;
        private bool _isFound;

        public string Code
        {
            get => _code;
            set => Set(ref _code, value.ToUpperInvariant().Trim());
        }

        public Exchange Exchange
        {
            get => _exchange;
            set => Set(ref _exchange, value);
        }

        public string Name
        {
            get => _name;
            set => Set(ref _name, value);
        }

        public double LatestPrice
        {
            get => _latestPrice;
            set => Set(ref _latestPrice, value);
        }

        public bool IsFound
        {
            get => _isFound;
            set => Set(ref _isFound, value);
        }
    }
}