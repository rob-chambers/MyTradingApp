using GalaSoft.MvvmLight;
using System;

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
        private double _priceIncrement;
        private int _quantityInterval = 1;

        public OrderItem()
        {
            PriceIncrement = 0.05;
        }

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
            set
            {
                Set(ref _quantity, value);
                QuantityInterval = _quantity >= 5000
                    ? 10
                    : _quantity >= 1000
                        ? 5
                        : 1;
            }
        }

        public double EntryPrice
        {
            get => _entryPrice;
            set
            {
                Set(ref _entryPrice, value);
                PriceIncrement = Math.Round(_entryPrice * 0.005, 2);
            }
        }

        public double PriceIncrement
        {
            get => _priceIncrement;
            set => Set(ref _priceIncrement, value);
        }

        public double InitialStopLossPrice
        {
            get => _initialStopLossPrice;
            set => Set(ref _initialStopLossPrice, value);
        }        

        public int QuantityInterval
        {
            get { return _quantityInterval; }
            set 
            { 
                Set(ref _quantityInterval, value); 
            }
        }

        public OrderStatus Status 
        { 
            get => _status;
            set => Set(ref _status, value);
        }
    }
}
