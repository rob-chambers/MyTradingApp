using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using MyTradingApp.Models;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Windows;

namespace MyTradingApp.ViewModels
{
    internal class OrdersViewModel : ObservableObject
    {
        public OrdersViewModel()
        {
            Debug.WriteLine("Instantiating Orders vm");
            Orders = new ObservableCollection<OrderItem>
            {
                new OrderItem
                {
                    Direction = Direction.Buy,
                    EntryPrice = 16.11D,
                    InitialStopLossPrice = 15.03,
                    Quantity = 100,
                    Symbol = new Symbol
                    {
                        Code = "JKS",
                        Exchange = Exchange.NYSE,
                        Name = "JinkoSolar"
                    }
                }
            };

            PopulateDirectionList();
            PopulateExchangeList();
        }

        private void PopulateDirectionList()
        {
            DirectionList = new ObservableCollection<Direction>();
            var values = Enum.GetValues(typeof(Direction));
            foreach (var value in values)
            {
                DirectionList.Add((Direction)value);
            }
        }

        private void PopulateExchangeList()
        {
            ExchangeList = new ObservableCollection<Exchange>();
            var values = Enum.GetValues(typeof(Exchange));
            foreach (var value in values)
            {
                ExchangeList.Add((Exchange)value);
            }
        }

        private RelayCommand _addCommand;
        private RelayCommand<OrderItem> _deleteCommand;
        private RelayCommand<OrderItem> _findCommand;
        private RelayCommand<OrderItem> _submitCommand;

        public ObservableCollection<OrderItem> Orders
        {
            get;
            private set;
        }

        public ObservableCollection<Direction> DirectionList { get; private set; }

        public ObservableCollection<Exchange> ExchangeList { get; private set; }

        public RelayCommand AddCommand
        {
            get
            {
                return _addCommand ?? (_addCommand = new RelayCommand(() =>
                {
                    Orders.Add(new OrderItem());
                }));
            }
        }

        public RelayCommand<OrderItem> FindCommand
        {
            get
            {
                return _findCommand ?? (_findCommand = new RelayCommand<OrderItem>(order =>
                {
                    MessageBox.Show("Find command fired for symbol " + order.Symbol.Code);
                }, order => CanFindOrder(order)));
            }
        }

        public RelayCommand<OrderItem> SubmitCommand
        {
            get
            {
                return _submitCommand ?? (_submitCommand = new RelayCommand<OrderItem>(order =>
                {
                    MessageBox.Show("Submit command fired for symbol " + order.Symbol.Code);
                }, order => CanSubmitOrder(order)));
            }
        }

        private bool CanSubmitOrder(OrderItem order)
        {
            return !string.IsNullOrEmpty(order.Symbol.Code);
        }

        private bool CanFindOrder(OrderItem order)
        {
            return !string.IsNullOrEmpty(order.Symbol.Code);
        }

        public RelayCommand<OrderItem> DeleteCommand
        {
            get
            {
                return _deleteCommand ?? (_deleteCommand = new RelayCommand<OrderItem>(
                    order =>
                    {
                        if (Orders.Contains(order))
                        {
                            Orders.Remove(order);
                        }
                    },
                    item => item != null));
            }
        }

        /*
         *         public Symbol Symbol { get; set; }

        public Direction Direction { get; set; }

        public double Quantity { get; set; }

        public double EntryPrice { get; set; }

        public double InitialStopLossPrice { get; set; }
         */

    }
}
