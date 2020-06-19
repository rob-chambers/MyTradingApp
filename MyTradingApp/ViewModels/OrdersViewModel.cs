using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using IBApi;
using MyTradingApp.Models;
using MyTradingApp.Services;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;

namespace MyTradingApp.ViewModels
{
    internal class OrdersViewModel : ObservableObject
    {
        private readonly IContractManager _contractManager;
        private readonly IMarketDataManager _marketDataManager;
        private RelayCommand _addCommand;
        private RelayCommand<OrderItem> _deleteCommand;
        private RelayCommand<OrderItem> _findCommand;
        private RelayCommand<OrderItem> _submitCommand;
        private RelayCommand _startStopStreamingCommand;
        private OrderItem _requestedOrder;
        private bool _isStreaming;
        private string _streamingButtonCaption;

        public OrdersViewModel(IContractManager contractManager, IMarketDataManager marketDataManager)
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
            _contractManager = contractManager;
            _marketDataManager = marketDataManager;
            _contractManager.FundamentalData += OnContractManagerFundamentalData;
            SetStreamingButtonCaption();
        }

        private void OnContractManagerFundamentalData(object sender, FundamentalDataEventArgs e)
        {
            _requestedOrder.Symbol.Name = e.Data.CompanyName;          
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
                    var order = new OrderItem();
                    order.Symbol.PropertyChanged += OnSymbolPropertyChanged;
                    Orders.Add(order);
                }));
            }
        }

        private void OnSymbolPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(Symbol.Code))
            {
                FindCommand.RaiseCanExecuteChanged();
            }
            else if (e.PropertyName == nameof(Symbol.Exchange))
            {
                FindCommand.RaiseCanExecuteChanged();
            }
        }

        public RelayCommand<OrderItem> FindCommand
        {
            get
            {
                return _findCommand ?? (_findCommand = new RelayCommand<OrderItem>(order => 
                    IssueFindSymbolRequest(order), order => CanFindOrder(order)));
            }
        }

        public RelayCommand StartStopStreamingCommand
        {
            get
            {
                return _startStopStreamingCommand ?? 
                    (_startStopStreamingCommand = new RelayCommand(StartStopStreaming));
            }
        }

        private void StartStopStreaming()
        {
            IsStreaming = !IsStreaming;
            if (IsStreaming)
            {
                GetMarketData();
            }
            else
            {
                CancelStreaming();
            }
        }

        private void GetMarketData()
        {
            var contract = MapOrderToContract(_requestedOrder);
            var genericTickList = string.Empty;
            _marketDataManager.AddRequest(contract, genericTickList);
        }

        private void CancelStreaming()
        {
            _marketDataManager.StopActiveRequests();
        }

        private bool CanStartStopStreaming()
        {
            return true;
        }

        private void IssueFindSymbolRequest(OrderItem order)
        {
            _requestedOrder = order;
            order.Symbol.Name = string.Empty;
            _contractManager.RequestFundamentals(MapOrderToContract(order), "ReportSnapshot");
        }

        public RelayCommand<OrderItem> SubmitCommand
        {
            get
            {
                return _submitCommand ?? (_submitCommand = new RelayCommand<OrderItem>(order =>
                {
                    order.Status = OrderStatus.Submitted;
                }, order => CanSubmitOrder(order)));
            }
        }

        private bool CanSubmitOrder(OrderItem order)
        {
            return !string.IsNullOrEmpty(order.Symbol.Name) && order.Status == OrderStatus.Pending;
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
                    order => order?.Status == OrderStatus.Pending));
            }
        }

        private static Contract MapOrderToContract(OrderItem order)
        {
            var contract = new Contract
            {
                Symbol = order.Symbol.Code,
                SecType = "STK",
                Exchange = order.Symbol.Exchange.ToString(),
                Currency = "USD",
                LastTradeDateOrContractMonth = string.Empty,
                Strike = 0,
                Multiplier = string.Empty,
                LocalSymbol = string.Empty
            };

            return contract;
        }
        //private Contract GetMDContract()
        //{
        //    Contract contract = new Contract();
        //    contract.LastTradeDateOrContractMonth = this.lastTradeDateOrContractMonth_TMD_MDT.Text;
        //    contract.PrimaryExch = this.primaryExchange.Text;
        //    contract.IncludeExpired = includeExpired.Checked;

        //    return contract;
        //}

        public bool IsStreaming
        {
            get => _isStreaming;
            set 
            {
                Set(ref _isStreaming, value);
                SetStreamingButtonCaption();
            }
        }

        public string StreamingButtonCaption
        {
            get => _streamingButtonCaption;
            set => Set(ref _streamingButtonCaption, value);
        }

        private void SetStreamingButtonCaption()
        {
            StreamingButtonCaption = IsStreaming
                ? "Cancel"
                : "Stream";
        }
    }
}
