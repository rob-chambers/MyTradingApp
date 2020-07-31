﻿using AutoFinance.Broker.InteractiveBrokers.Constants;
using AutoFinance.Broker.InteractiveBrokers.EventArgs;
using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Messaging;
using IBApi;
using MyTradingApp.Core;
using MyTradingApp.Core.Utils;
using MyTradingApp.Core.ViewModels;
using MyTradingApp.Domain;
using MyTradingApp.EventMessages;
using MyTradingApp.Repositories;
using MyTradingApp.Services;
using MyTradingApp.Utils;
using ObjectDumper;
using Serilog;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace MyTradingApp.ViewModels
{
    public class OrdersViewModel : DispatcherViewModel
    {
        #region Fields

        private const string YearMonthDayPattern = "yyyyMMdd";

        private readonly IContractManager _contractManager;
        private readonly IHistoricalDataManager _historicalDataManager;
        private readonly IMarketDataManager _marketDataManager;
        private readonly IOrderCalculationService _orderCalculationService;
        private readonly IOrderManager _orderManager;
        private readonly ITradeRepository _tradeRepository;
        private string _accountId;
        private CommandBase _addCommand;
        private CommandBase<OrderItem> _deleteCommand;
        private AsyncCommand<OrderItem> _findCommand;
        private bool _isStreaming;
        private AsyncCommand _startStopStreamingCommand;
        private RelayCommand _deleteAllCommand;
        private string _streamingButtonCaption;
        private AsyncCommand<OrderItem> _submitCommand;
        private OrderItem _selectedOrder;
        #endregion

        #region Constructors

        public OrdersViewModel(
            IDispatcherHelper dispatcherHelper,
            IContractManager contractManager, 
            IMarketDataManager marketDataManager,
            IHistoricalDataManager historicalDataManager,
            IOrderCalculationService orderCalculationService,
            IOrderManager orderManager,
            ITradeRepository tradeRepository,
            IQueueProcessor queueProcessor) 
            : base(dispatcherHelper, queueProcessor)
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
            _tradeRepository = tradeRepository;
            Messenger.Default.Register<OrderStatusChangedMessage>(this, OrderStatusChangedMessage.Tokens.Orders, OnOrderStatusChangedMessage);
            Messenger.Default.Register<AccountSummaryCompletedMessage>(this, HandleAccountSummaryMessage);
            Messenger.Default.Register<BarPriceMessage>(this, HandleBarPriceMessage);

            SetStreamingButtonCaption();
        }

        private void HandleBarPriceMessage(BarPriceMessage message)
        {
            if (!IsStreaming)
            {
                // It wasn't us that triggered the event
                return;
            }

            Log.Debug(message.DumpToString("Handling streaming bar price"));
            var orders = Orders.Where(o => o.Symbol.Code == message.Symbol).ToList();
            if (!orders.Any())
            {
                return;
            }

            if (orders.Count > 1)
            {
                Log.Warning("Found more than one order for {0} - taking the first", message.Symbol);
            }

            var order = orders.First();
            order.Symbol.LatestPrice = message.Bar.Close;
            _orderCalculationService.SetLatestPrice(message.Symbol, message.Bar.Close);
            CalculateRisk(message.Symbol);
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

        public CommandBase AddCommand
        {
            get
            {
                return _addCommand ?? (_addCommand = new CommandBase(DispatcherHelper, () =>
                {
                    var order = new OrderItem();
                    order.PropertyChanged += OnItemPropertyChanged;
                    order.Symbol.PropertyChanged += OnSymbolPropertyChanged;
                    Orders.Add(order);              
                }));
            }
        }

        public CommandBase<OrderItem> DeleteCommand
        {
            get
            {
                return _deleteCommand ?? (_deleteCommand = new CommandBase<OrderItem>(DispatcherHelper,
                    order =>
                    {
                        if (Orders.Contains(order))
                        {
                            order.Symbol.PropertyChanged -= OnSymbolPropertyChanged;
                            order.PropertyChanged -= OnItemPropertyChanged;
                            Orders.Remove(order);
                        }
                    },
                    order => CanDelete(order)));
            }
        }

        private bool CanDelete(OrderItem order)
        {
            return order?.Status == OrderStatus.Pending || order?.Status == OrderStatus.Cancelled;
        }

        public ObservableCollection<Direction> DirectionList { get; private set; }

        public ObservableCollection<Exchange> ExchangeList { get; private set; }

        public AsyncCommand<OrderItem> FindCommand
        {
            get
            {
                return _findCommand ?? (_findCommand = new AsyncCommand<OrderItem>(
                    DispatcherHelper, 
                    order => FindSymbolAndProcessAsync(order), 
                    order => CanFindOrder(order)));
            }
        }

        private async Task FindSymbolAndProcessAsync(OrderItem order)
        {
            var results = await IssueFindSymbolRequestAsync(order).ConfigureAwait(false);
            if (results == null)
            {
                return;
            }

            var details = results.Details;
            var symbol = order.Symbol.Code;
            if (!details.Any())
            {
                Log.Warning("No contract details returned for {0}", symbol);
                return;
            }

            if (details.Count > 1)
            {
                Log.Warning("Found multiple contract detail items for {0} - taking the first", symbol);
            }

            var detail = details.First();
            order.Symbol.IsFound = true;

            order.Symbol.LatestPrice = results.LatestPrice;
            _orderCalculationService.SetLatestPrice(order.Symbol.Code, results.LatestPrice);
            CalculateRisk(order.Symbol.Code);
            ProcessHistory(order, results.PriceHistory);

            if (IsStreaming)
            {
                await StreamSymbolAsync(order).ConfigureAwait(false);
            }

            DispatcherHelper.InvokeOnUiThread(() =>
            {
                StartStopStreamingCommand.RaiseCanExecuteChanged();
                SubmitCommand.RaiseCanExecuteChanged();
            });

            order.Symbol.MinTick = detail.MinTick;
            order.Symbol.Name = detail.LongName;
        }

        private void ProcessHistory(OrderItem order, List<HistoricalDataEventArgs> results)
        {
            if (results.Any())
            {
                order.HasHistory = true;

                var bars = new BarCollection();
                foreach (var item in results.Select(x => new Domain.Bar
                {
                    Date = DateTime.ParseExact(x.Date, YearMonthDayPattern, new CultureInfo("en-US")),
                    Open = x.Open,
                    High = x.High,
                    Low = x.Low,
                    Close = x.Close
                }).OrderByDescending(x => x.Date))
                {
                    bars.Add(item.Date, item);
                }

                _orderCalculationService.SetHistoricalData(order.Symbol.Code, bars);
                CalculateRisk(order.Symbol.Code);
            }
            else
            {
                Log.Debug("No hostorical data found");
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

        public AsyncCommand StartStopStreamingCommand
        {
            get
            {
                return _startStopStreamingCommand ??
                    (_startStopStreamingCommand = new AsyncCommand(DispatcherHelper, StartStopStreamingAsync, CanStartStopStreaming));
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
            foreach (var order in Orders.Where(o => CanDelete(o)).ToList())
            {
                Orders.Remove(order);
            }
        }

        public string StreamingButtonCaption
        {
            get => _streamingButtonCaption;
            set => Set(ref _streamingButtonCaption, value);
        }

        public AsyncCommand<OrderItem> SubmitCommand
        {
            get
            {
                return _submitCommand ?? (_submitCommand = new AsyncCommand<OrderItem>(DispatcherHelper, SubmitOrderAsync, order => CanSubmitOrder(order)));
            }
        }

        public OrderItem SelectedOrder
        {
            get => _selectedOrder;
            set
            {
                Set(ref _selectedOrder, value);
                Messenger.Default.Send(new OrderSelectionChangedMessage(value));
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
                PrimaryExch = IbClientRequestHelper.MapExchange(order.Symbol.Exchange),
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

            order.StandardDeviation = _orderCalculationService.CalculateStandardDeviation(symbol);
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

        private async Task GetMarketDataAsync()
        {
            foreach (var item in Orders)
            {
                await StreamSymbolAsync(item);
            }
        }

        private async Task StreamSymbolAsync(OrderItem item)
        {
            var contract = MapOrderToContract(item);
            //_marketDataManager.RequestStreamingPrice(contract);
            await _marketDataManager.RequestStreamingPriceAsync(contract);
        }

        private Order GetPrimaryOrder(OrderItem orderItem)
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
            order.AuxPrice = Rounding.ValueAdjustedForMinTick(stopPrice, orderItem.Symbol.MinTick);
            order.TotalQuantity = orderItem.Quantity;
            order.Account = _accountId;
            order.ModelCode = string.Empty;
            order.Tif = BrokerConstants.TimeInForce.Day;
            return order;
        }

        private Order GetTrailingStopOrder(OrderItem orderItem)
        {
            var stopOrder = GetInitialStopOrder(orderItem);
            stopOrder.OrderType = BrokerConstants.OrderTypes.Trail;
            stopOrder.AuxPrice = 0;
            stopOrder.TrailingPercent = 5;
            stopOrder.ParentId = 0;

            return stopOrder;
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

            var stopPrice = Rounding.ValueAdjustedForMinTick(orderItem.InitialStopLossPrice, orderItem.Symbol.MinTick);
            order.AuxPrice = stopPrice;
            order.TotalQuantity = orderItem.Quantity;
            order.Account = _accountId;
            order.ModelCode = string.Empty;
            order.Tif = BrokerConstants.TimeInForce.GoodTilCancelled;
            order.Transmit = true;
            return order;
        }

        private void HandleAccountSummaryMessage(AccountSummaryCompletedMessage message)
        {
            _accountId = message.AccountId;
        }

        private async Task<FindCommandResultsModel> IssueFindSymbolRequestAsync(OrderItem order)
        {
            var symbol = order.Symbol.Code;
            if (Orders.Any(x => x != order && x.Symbol.Code == symbol))
            {
                Messenger.Default.Send(new NotificationMessage<NotificationType>(NotificationType.Warning, $"There is already an order for {symbol}."));
                return null;
            }

            order.Symbol.IsFound = false;
            order.Symbol.Name = string.Empty;

            var model = new FindCommandResultsModel();            

            var contract = MapOrderToContract(order);
            var getLatestPriceTask = _marketDataManager.RequestLatestPriceAsync(contract);
            var getHistoryTask = _historicalDataManager.GetHistoricalDataAsync(
                MapOrderToContract(order), DateTime.UtcNow, TwsDuration.OneMonth, TwsBarSizeSetting.OneDay, TwsHistoricalDataRequestType.Midpoint);
            var detailsTask = _contractManager.RequestDetailsAsync(contract);

            await Task.WhenAll(getLatestPriceTask, getHistoryTask, detailsTask).ConfigureAwait(false);

            model.LatestPrice = await getLatestPriceTask;
            model.PriceHistory = await getHistoryTask;
            model.Details = await detailsTask;

            return model;
        }

        private async void OnOrderStatusChangedMessage(OrderStatusChangedMessage message)
        {
            // Find corresponding order
            var order = Orders.SingleOrDefault(o => o.Id == message.Message.OrderId);
            if (order == null)
            {
                // Most likely an existing pending order (i.e. one that wasn't submitted via this app while it is currently open)
                return;
            }

            UpdateOrderStatus(order, message.Message.Status);
            if (order.Status == OrderStatus.Filled)
            {
                Log.Debug("A new order for {0} was filled", order.Symbol.Code);
                var addTradeTask = AddTradeAsync(order, message.Message.AvgFillPrice);
                var stopOrderTask = SubmitStopOrderAsync(order, message.Message);

                await Task.WhenAll(addTradeTask, stopOrderTask).ConfigureAwait(false);

                // This order can be removed now that it is dealt with - it will be added as a position
                DispatcherHelper.InvokeOnUiThread(() => Orders.Remove(order));

                // Pass this message on to the positions vm now that we have a stop order 
                Messenger.Default.Send(message, OrderStatusChangedMessage.Tokens.Positions);
            }
        }

        private Task AddTradeAsync(OrderItem order, double fillPrice)
        {
            Log.Debug("Recording trade");
            return _tradeRepository.AddTradeAsync(new Trade
            {
                Symbol = order.Symbol.Code,
                Direction = order.Direction,
                EntryPrice = fillPrice,
                EntryTimeStamp = DateTime.UtcNow,
                Quantity = order.Quantity
            });
        }

        private Task SubmitStopOrderAsync(OrderItem order, OrderStatusEventArgs args)
        {
            Log.Information("Submitting stop order");
            var stopOrder = GetTrailingStopOrder(order);
            stopOrder.TotalQuantity = args.Filled;
            var contract = MapOrderToContract(order);
            return _orderManager.PlaceNewOrderAsync(contract, stopOrder);
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

        private void SetStreamingButtonCaption()
        {
            StreamingButtonCaption = IsStreaming
                ? "Stop Streaming"
                : "Start Streaming";
        }

        private async Task StartStopStreamingAsync()
        {
            IsStreaming = !IsStreaming;
            if (IsStreaming)
            {
                await GetMarketDataAsync();
            }
            else
            {
                CancelStreaming();
            }
        }

        private async Task SubmitOrderAsync(OrderItem orderItem)
        {
            var contract = MapOrderToContract(orderItem);
            contract.LocalSymbol = orderItem.Symbol.Code;

            var order = GetPrimaryOrder(orderItem);            
            await _orderManager.PlaceNewOrderAsync(contract, order);
            orderItem.Id = order.OrderId;

            //// Attach stop order
            //var stopOrder = GetInitialStopOrder(orderItem);
            //_orderManager.PlaceNewOrder(contract, stopOrder);

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

            DispatcherHelper.InvokeOnUiThread(() =>
            {
                FindCommand.RaiseCanExecuteChanged();
                SubmitCommand.RaiseCanExecuteChanged();
                DeleteCommand.RaiseCanExecuteChanged();
            });
        }

        #endregion
    }
}
