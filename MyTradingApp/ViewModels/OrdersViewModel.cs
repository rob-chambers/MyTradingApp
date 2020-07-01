using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Messaging;
using IBApi;
using MyTradingApp.EventMessages;
using MyTradingApp.Models;
using MyTradingApp.Services;
using MyTradingApp.Utils;
using ObjectDumper;
using Serilog;
using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;

namespace MyTradingApp.ViewModels
{
    internal class OrdersViewModel : ObservableObject
    {
        #region Fields

        private readonly IContractManager _contractManager;
        private readonly IHistoricalDataManager _historicalDataManager;
        private readonly IMarketDataManager _marketDataManager;
        private readonly IOrderCalculationService _orderCalculationService;
        private readonly IOrderManager _orderManager;

        private string _accountId;
        private RelayCommand _addCommand;
        private RelayCommand<OrderItem> _deleteCommand;
        private RelayCommand<OrderItem> _findCommand;
        private bool _isStreaming;
        private RelayCommand _startStopStreamingCommand;
        private RelayCommand _deleteAllCommand;
        private string _streamingButtonCaption;
        private RelayCommand<OrderItem> _submitCommand;
        #endregion

        #region Constructors

        public OrdersViewModel(
            IContractManager contractManager, 
            IMarketDataManager marketDataManager,
            IHistoricalDataManager historicalDataManager,
            IOrderCalculationService orderCalculationService,
            IOrderManager orderManager)
        {
            Orders = new ObservableCollectionNoReset<OrderItem>();
            Orders.CollectionChanged += OnOrdersCollectionChanged;
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

        private void OnOrdersCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action != NotifyCollectionChangedAction.Remove)
            {
                return;
            }

            foreach (OrderItem item in e.OldItems)
            {
                item.PropertyChanged -= OnItemPropertyChanged;
                item.Symbol.PropertyChanged -= OnSymbolPropertyChanged;

                Messenger.Default.Send(new OrderRemovedMessage(item));
                if (IsStreaming)
                {
                    _marketDataManager.StopPriceStreaming(item.Symbol.Code);
                }                    
            }
        }

        #endregion

        #region Properties

        public RelayCommand AddCommand
        {
            get
            {
                return _addCommand ?? (_addCommand = new RelayCommand(() =>
                {
                    var order = new OrderItem();
                    order.PropertyChanged += OnItemPropertyChanged;
                    order.Symbol.PropertyChanged += OnSymbolPropertyChanged;
                    Orders.Add(order);              
                }));
            }
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
                            order.Symbol.PropertyChanged -= OnSymbolPropertyChanged;
                            order.PropertyChanged -= OnItemPropertyChanged;
                            Orders.Remove(order);
                        }
                    },
                    order => order?.Status == OrderStatus.Pending || order?.Status == OrderStatus.Cancelled));
            }
        }

        public ObservableCollection<Direction> DirectionList { get; private set; }

        public ObservableCollection<Exchange> ExchangeList { get; private set; }

        public RelayCommand<OrderItem> FindCommand
        {
            get
            {
                return _findCommand ?? (_findCommand = new RelayCommand<OrderItem>(order =>
                    IssueFindSymbolRequest(order), order => CanFindOrder(order)));
            }
        }

        public bool IsStreaming
        {
            get => _isStreaming;
            set
            {
                Set(ref _isStreaming, value);
                Messenger.Default.Send(new StreamingChangedMessage(value));
                SetStreamingButtonCaption();
            }
        }

        public ObservableCollectionNoReset<OrderItem> Orders
        {
            get;
            private set;
        }

        public RelayCommand StartStopStreamingCommand
        {
            get
            {
                return _startStopStreamingCommand ??
                    (_startStopStreamingCommand = new RelayCommand(StartStopStreaming, CanStartStopStreaming));
            }
        }

        public RelayCommand DeleteAllCommand
        {
            get
            {
                return _deleteAllCommand ?? (_deleteAllCommand = new RelayCommand(DeleteAll));
            }
        }

        private void DeleteAll()
        {
            Orders.Clear();
        }

        public string StreamingButtonCaption
        {
            get => _streamingButtonCaption;
            set => Set(ref _streamingButtonCaption, value);
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

        #endregion

        #region Methods

        private static Contract MapOrderToContract(OrderItem order)
        {
            var contract = new Contract
            {
                Symbol = order.Symbol.Code,
                SecType = BrokerConstants.Stock,
                Exchange = BrokerConstants.Routers.Smart,
                PrimaryExch = MapExchange(order.Symbol.Exchange),
                Currency = BrokerConstants.UsCurrency,
                LastTradeDateOrContractMonth = string.Empty,
                Strike = 0,
                Multiplier = string.Empty,
                LocalSymbol = string.Empty
            };

            return contract;
        }

        public void RecalculateRiskForAllOrders()
        {
            foreach (var item in Orders)
            {
                CalculateRisk(item.Symbol.Code);
            }
        }

        private static string MapExchange(Exchange exchange)
        {
            switch (exchange)
            {
                // On https://interactivebrokers.github.io/tws-api/basic_contracts.html, it mentions that stocks on the Nasdaq should be routed through ISLAND
                case Exchange.Nasdaq:
                    return BrokerConstants.Routers.Island;

                case Exchange.London:
                    return "LSE";
            }

            return exchange.ToString();
        }

        private void CalculateRisk(string symbol)
        {
            if (!_orderCalculationService.CanCalculate(symbol))
            {
                return;
            }

            var order = Orders.SingleOrDefault(o => o.Symbol.Code == symbol);
            if (order == null)
            {
                return;
            }

            var sl = _orderCalculationService.CalculateInitialStopLoss(symbol, order.Direction);

            order.EntryPrice = _orderCalculationService.GetEntryPrice(symbol, order.Direction);
            order.InitialStopLossPrice = sl;
            order.Quantity = _orderCalculationService.GetCalculatedQuantity(symbol, order.Direction);
        }

        private void CancelStreaming()
        {
            _marketDataManager.StopActivePriceStreaming();
        }

        private bool CanFindOrder(OrderItem order)
        {
            return !string.IsNullOrEmpty(order.Symbol.Code) && !order.IsLocked;
        }

        private bool CanStartStopStreaming()
        {
            return IsStreaming || Orders.Any(o => o.Symbol.IsFound);
        }

        private bool CanSubmitOrder(OrderItem order)
        {
            return order.Symbol.IsFound && !order.IsLocked;
        }

        private void GetMarketData()
        {
            foreach (var item in Orders)
            {
                StreamSymbol(item);
            }
        }

        private void StreamSymbol(OrderItem item)
        {
            var contract = MapOrderToContract(item);
            _marketDataManager.RequestStreamingPrice(contract);
        }

        private Order GetOrder(OrderItem orderItem)
        {
            var order = new Order();
            if (orderItem.Id != 0)
            {
                order.OrderId = orderItem.Id;
            }

            order.Action = orderItem.Direction == Direction.Buy
                ? BrokerConstants.Actions.Buy
                : BrokerConstants.Actions.Sell;

            order.OrderType = BrokerConstants.OrderTypes.Stop;

            var stopPrice = orderItem.EntryPrice;
            order.AuxPrice = stopPrice;
            order.TotalQuantity = orderItem.Quantity;
            order.Account = _accountId;
            order.ModelCode = string.Empty;
            order.Tif = BrokerConstants.TimeInForce.Day;
            return order;
        }

        private Order GetInitialStopOrder(OrderItem orderItem)
        {
            var order = new Order();
            if (orderItem.Id != 0)
            {
                order.ParentId = orderItem.Id;
            }

            // Action for a Stop order will be the opposite
            order.Action = orderItem.Direction == Direction.Buy
                ? BrokerConstants.Actions.Sell
                : BrokerConstants.Actions.Buy;

            order.OrderType = BrokerConstants.OrderTypes.Stop;

            var stopPrice = orderItem.InitialStopLossPrice;
            order.AuxPrice = stopPrice;
            order.TotalQuantity = orderItem.Quantity;
            order.Account = _accountId;
            order.ModelCode = string.Empty;
            order.Tif = BrokerConstants.TimeInForce.GoodTilCancelled;

            // TODO: Test this will work
            order.Transmit = true;
            return order;
        }

        private void HandleAccountSummaryMessage(AccountSummaryCompletedMessage message)
        {
            _accountId = message.AccountId;
        }

        private void HandleTickPriceMessage(TickPrice tickPrice)
        {
            var order = Orders.SingleOrDefault(o => o.Symbol.Code == tickPrice.Symbol);
            if (order == null)
            {
                return;
            }

            order.Symbol.LatestPrice = tickPrice.Price;
            _orderCalculationService.SetLatestPrice(tickPrice.Symbol, tickPrice.Price);
            CalculateRisk(tickPrice.Symbol);

            if (!order.Symbol.IsFound)
            {                
                order.Symbol.IsFound = true;
                IssueHistoricalDataRequest(order);
                StartStopStreamingCommand.RaiseCanExecuteChanged();
                SubmitCommand.RaiseCanExecuteChanged();                

                if (IsStreaming)
                {
                    StreamSymbol(order);
                }
            }
        }

        private void IssueFindSymbolRequest(OrderItem order)
        {
            var symbol = order.Symbol.Code;
            if (Orders.Any(x => x != order && x.Symbol.Code == symbol))
            {
                Messenger.Default.Send(new NotificationMessage<NotificationType>(NotificationType.Warning, $"There is already an order for {symbol}."));
                return;
            }

            order.Symbol.IsFound = false;
            order.Symbol.Name = string.Empty;
            RequestLatestPrice(order);
            _contractManager.RequestFundamentals(MapOrderToContract(order), "ReportSnapshot");
        }

        private void IssueHistoricalDataRequest(OrderItem order)
        {
            Log.Debug($"Issuing historical data request for {order.Symbol.Code}");
            var endTime = DateTime.Now.ToString(HistoricalDataManager.FullDatePattern);
            _historicalDataManager.AddRequest(MapOrderToContract(order), endTime, "25 D", "1 day", "MIDPOINT", 0, 1, false);
        }

        private void OnContractManagerFundamentalData(FundamentalDataMessage message)
        {
            var order = Orders.SingleOrDefault(x => x.Symbol.Code == message.Symbol);
            if (order == null)
            {
                return;
            }

            Log.Debug($"Found fundamental data for {order.Symbol.Code}");
            order.Symbol.IsFound = true;
            order.Symbol.Name = message.Data.CompanyName;
            order.Symbol.CompanyDescription = message.Data.CompanyDescription;
            //IssueHistoricalDataRequest(order);
            //StartStopStreamingCommand.RaiseCanExecuteChanged();
            //SubmitCommand.RaiseCanExecuteChanged();

            //RequestLatestPrice(order);

            //if (IsStreaming)
            //{
            //    StreamSymbol(order);
            //}
        }

        private void OnHistoricalDataManagerDataCompleted(HistoricalDataCompletedMessage message)
        {
            var order = Orders.SingleOrDefault(x => x.Symbol.Code == message.Symbol);
            if (order == null)
            {
                return;
            }

            _orderCalculationService.SetHistoricalData(order.Symbol.Code, message.Bars);
            CalculateRisk(order.Symbol.Code);
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

            var value = message.DumpToString("Order Status Message");
            Log.Debug(value);

            UpdateOrderStatus(order, message.Message.Status);
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

        private void OnItemPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName != nameof(OrderItem.Direction))
            {
                return;
            }

            var order = (OrderItem)sender;
            CalculateRisk(order.Symbol.Code);
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

        private void RequestLatestPrice(OrderItem order)
        {
            var contract = MapOrderToContract(order);
            _marketDataManager.RequestLatestPrice(contract);
        }

        private void SetStreamingButtonCaption()
        {
            StreamingButtonCaption = IsStreaming
                ? "Stop Streaming"
                : "Start Streaming";
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

        private void SubmitOrder(OrderItem orderItem)
        {
            var contract = MapOrderToContract(orderItem);
            contract.LocalSymbol = orderItem.Symbol.Code;

            var order = GetOrder(orderItem);

            _orderManager.PlaceNewOrder(contract, order);
            orderItem.Id = order.OrderId;

            // Attach stop order
            var stopOrder = GetInitialStopOrder(orderItem);
            _orderManager.PlaceNewOrder(contract, stopOrder);

            // Transition from a TRAIL to a standard stop
            //var newOrder = GetInitialStopOrder(orderItem);
            //newOrder.OrderId = stopOrderId;
            //var newOrderType = BrokerConstants.OrderTypes.Stop;
            //var triggerPrice = (orderItem.EntryPrice - orderItem.InitialStopLossPrice) * 1.1 + orderItem.EntryPrice;
            //var newStopPrice = orderItem.EntryPrice;

            //newOrder.TriggerPrice = triggerPrice;
            //newOrder.AdjustedOrderType = newOrderType;
            //newOrder.AdjustedStopPrice = newStopPrice;
            //newOrder.Transmit = true;

            //_orderManager.PlaceNewOrder(contract, newOrder);
        }

        private void UpdateOrderStatus(OrderItem order, string status)
        {
            switch (status)
            {
                case BrokerConstants.OrderStatus.PreSubmitted:
                    order.Status = OrderStatus.PreSubmitted;
                    break;

                case BrokerConstants.OrderStatus.Submitted:
                    order.Status = OrderStatus.Submitted;
                    break;

                case BrokerConstants.OrderStatus.Cancelled:
                    order.Status = OrderStatus.Cancelled;
                    break;

                case BrokerConstants.OrderStatus.Filled:
                    order.Status = OrderStatus.Filled;
                    break;

                default:
                    Log.Warning("Status that isn't handled: {0}", status);
                    if (Debugger.IsAttached)
                    {
                        break;
                    }
                    break;
            }

            FindCommand.RaiseCanExecuteChanged();
            SubmitCommand.RaiseCanExecuteChanged();
            DeleteCommand.RaiseCanExecuteChanged();
        }

        #endregion
    }
}
