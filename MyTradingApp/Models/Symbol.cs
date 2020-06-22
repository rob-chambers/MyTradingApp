using GalaSoft.MvvmLight;

namespace MyTradingApp.Models
{
    internal class Symbol : ViewModelBase
    {
        private string _code;
        private Exchange _exchange;
        private string _name;
        private double _latestPrice;

        public string Code
        {
            get => _code;
            set => Set(ref _code, value.ToUpperInvariant());
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
    }
}