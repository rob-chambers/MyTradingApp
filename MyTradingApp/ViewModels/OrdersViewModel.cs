using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Messaging;
using IBApi;
using MyTradingApp.EventMessages;
using MyTradingApp.Models;
using MyTradingApp.Services;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;

namespace MyTradingApp.ViewModels
{
    internal class OrdersViewModel : ObservableObject
    {
        private readonly IContractManager _contractManager;
        private readonly IMarketDataManager _marketDataManager;
        private readonly IHistoricalDataManager _historicalDataManager;
        private readonly IOrderCalculationService _orderCalculationService;
        private readonly IOrderManager _orderManager;
        private RelayCommand _addCommand;
        private RelayCommand<OrderItem> _deleteCommand;
        private RelayCommand<OrderItem> _findCommand;
        private RelayCommand<OrderItem> _submitCommand;
        private RelayCommand _startStopStreamingCommand;
        private OrderItem _requestedOrder;
        private bool _isStreaming;
        private string _streamingButtonCaption;
        private int _orderId;
        private int _parentOrderId;
        private string _accountId;


        public OrdersViewModel(
            IContractManager contractManager, 
            IMarketDataManager marketDataManager,
            IHistoricalDataManager historicalDataManager,
            IOrderCalculationService orderCalculationService,
            IOrderManager orderManager)
        {
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
            _historicalDataManager = historicalDataManager;
            _orderCalculationService = orderCalculationService;
            _orderManager = orderManager;            
            Messenger.Default.Register<FundamentalDataMessage>(this, OnContractManagerFundamentalData);
            Messenger.Default.Register<HistoricalDataCompletedMessage>(this, OnHistoricalDataManagerDataCompleted);
            Messenger.Default.Register<OrderStatusChangedMessage>(this, OnOrderStatusChangedMessage);
            Messenger.Default.Register<AccountSummaryCompletedMessage>(this, HandleAccountSummaryMessage);
            Messenger.Default.Register<TickPrice>(this, HandleTickPriceMessage);
            SetStreamingButtonCaption();
        }

        private void HandleTickPriceMessage(TickPrice tickPrice)
        {
            var order = Orders.SingleOrDefault(o => o.Symbol.Code == tickPrice.Symbol);
            if (order == null)
            {
                return;
            }

            order.Symbol.LatestPrice = tickPrice.Price;
        }

        private void HandleAccountSummaryMessage(AccountSummaryCompletedMessage message)
        {
            _accountId = message.AccountId;
        }

        private void OnHistoricalDataManagerDataCompleted(HistoricalDataCompletedMessage message)
        {            
            _orderCalculationService.SetHistoricalData(message.Bars);
            var sl = _orderCalculationService.CalculateInitialStopLoss();

            _requestedOrder.EntryPrice = message.Bars.First().Close;
            _requestedOrder.InitialStopLossPrice = sl;
            _requestedOrder.Quantity = _orderCalculationService.GetCalculatedQuantity();
        }

        private void OnOrderStatusChangedMessage(OrderStatusChangedMessage message)
        {
            // Find corresponding order
            var order = Orders.SingleOrDefault(o => o.Id == message.Message.OrderId);
            if (order == null)
            {
                // Most likely an existing pending order (i.e. one that wasn't submitted via this app while it is currently open)
                return;
            }

            Debug.WriteLine("Order status: {0}", message.Message.Status);

            UpdateOrderStatus(order, message.Message.Status);
        }

        private void UpdateOrderStatus(OrderItem order, string status)
        {
            switch (status)
            {
                case "PreSubmitted":
                    order.Status = OrderStatus.Submitted;
                    break;

                case "Cancelled":
                    order.Status = OrderStatus.Cancelled;
                    break;
            }
        }

        private void OnContractManagerFundamentalData(FundamentalDataMessage message)
        {
            _requestedOrder.Symbol.Name = message.Data.CompanyName;
            Messenger.Default.Send(new GenericMessage<OrderItem>(_requestedOrder));
            IssueHistoricalDataRequest(_requestedOrder);
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
            if (_requestedOrder == null) return;

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

        private void IssueHistoricalDataRequest(OrderItem order)
        {
            _requestedOrder = order;
            //historicalDataManager.AddRequest(contract, endTime, duration, barSize, whatToShow, outsideRTH, 1, cbKeepUpToDate.Checked);

            var endTime = DateTime.Now.ToString(HistoricalDataManager.FullDatePattern);
            _historicalDataManager.AddRequest(MapOrderToContract(order), endTime, "25 D", "1 day", "MIDPOINT", 0, 1, false);
        }

        public RelayCommand<OrderItem> SubmitCommand
        {
            get
            {
                return _submitCommand ?? (_submitCommand = new RelayCommand<OrderItem>(order =>
                {
                    SubmitOrder(order);
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

        private void SubmitOrder(OrderItem orderItem)
        {
            var contract = MapOrderToContract(orderItem);
            contract.LocalSymbol = orderItem.Symbol.Code;
            contract.PrimaryExch = orderItem.Symbol.Exchange.ToString();
            contract.Exchange = "IDEALPRO";

            var order = GetOrder(orderItem);
            var id = _orderManager.PlaceNewOrder(contract, order);

            // Find this order in the collection and update its id
            var index = Orders.IndexOf(orderItem);
            if (index >= 0)
            {
                Orders[index].Id = id;
            }
        }

        private Order GetOrder(OrderItem orderItem)
        {
            var order = new Order();
            if (orderItem.Id != 0)
            {
                order.OrderId = orderItem.Id;
            }

            if (_parentOrderId != 0)
            {
                order.ParentId = _parentOrderId;
            }

            /* actions:
             * "BUY",
            "SELL",
            "SSHORT"});
            */

            order.Action = orderItem.Direction == Direction.Buy
                ? "BUY"
                : "SELL";

            /* Order types
            "MKT",
            "LMT",
            "STP",
            "STP LMT",
            "REL",
            "TRAIL",
            */

            order.OrderType = "STP";

            var stopPrice = orderItem.EntryPrice;
            order.AuxPrice = stopPrice;
            order.TotalQuantity = orderItem.Quantity;
            order.Account = _accountId;
            order.ModelCode = string.Empty;

            /* Time in force values              
            "DAY",
            "GTC",
            "OPG",
            "IOC",
            "GTD",
            "GTT",
            "AUC",
            "FOK",
            "GTX",
            "DTC" */

            order.Tif = "DAY";
            //FillExtendedOrderAttributes(order);
            //FillAdvisorAttributes(order);
            //FillVolatilityAttributes(order);
            //FillScaleAttributes(order);
            //FillAlgoAttributes(order);
            //FillPegToBench(order);
            //FillAdjustedStops(order);
            //FillConditions(order);

            return order;
        }
    }
}
