using GalaSoft.MvvmLight;

namespace MyTradingApp.Models
{
    internal class OrderItem : ViewModelBase
    {
        private Symbol _symbol = new Symbol();
        private Direction _direction;
        private double _quantity;
        private double _entryPrice;
        private double _initialStopLossPrice;
        private OrderStatus _status;
        
        public Symbol Symbol
        {
            get => _symbol;
            set => Set(ref _symbol, value);
        }

        public Direction Direction
        {
            get => _direction;
            set => Set(ref _direction, value);
        }

        public double Quantity
        {
            get => _quantity;
            set => Set(ref _quantity, value);
        }

        public double EntryPrice
        {
            get => _entryPrice;
            set => Set(ref _entryPrice, value);
        }

        public double InitialStopLossPrice
        {
            get => _initialStopLossPrice;
            set => Set(ref _initialStopLossPrice, value);
        }

        public OrderStatus Status 
        { 
            get => _status;
            set => Set(ref _status, value);
        }
    }
}
