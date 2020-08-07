using AutoFinance.Broker.InteractiveBrokers.EventArgs;
using GalaSoft.MvvmLight.Messaging;
using IBApi;
using MyTradingApp.Core.EventMessages;
using MyTradingApp.Core.Services;
using MyTradingApp.Core.Utils;
using MyTradingApp.Domain;
using Serilog;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace MyTradingApp.Core.ViewModels
{
    public class NewOrderViewModel : DispatcherViewModel
    {
        #region Fields
        public const string YearMonthDayPattern = "yyyyMMdd";

        private readonly IFindSymbolService _findSymbolService;
        private readonly IOrderCalculationService _orderCalculationService;
        private readonly IOrderManager _orderManager;
        private Direction _direction;
        private ushort _quantity = 1;
        private OrderStatus _status;
        private int _quantityInterval;
        private bool _isLocked;
        private double _priceIncrement = 0.05;        
        private AsyncCommand _findCommand;
        private bool _isBusy;
        private string _findCommandCaption = FindButtonCaptions.Default;
        private AsyncCommand _submitCommand;
        private bool _hasHistory;
        private double _entryPrice;
        private string _accountId;
        private double _initialStopLossPrice;
        private int _id;

        #endregion

        #region Nested Classes
        public static class FindButtonCaptions
        {
            public const string Default = "Find";
            public const string Finding = "Finding...";
        }

        #endregion

        #region ctor

        public NewOrderViewModel(
            IDispatcherHelper dispatcherHelper, 
            IQueueProcessor queueProcessor,
            IFindSymbolService findSymbolService,
            IOrderCalculationService orderCalculationService,
            IOrderManager orderManager)
            : base(dispatcherHelper, queueProcessor)
        {
            Symbol.PropertyChanged += OnSymbolPropertyChanged;
            _findSymbolService = findSymbolService;
            _orderCalculationService = orderCalculationService;
            _orderManager = orderManager;
            Messenger.Default.Register<AccountSummaryCompletedMessage>(this, msg => _accountId = msg.AccountId);
            Messenger.Default.Register<OrderStatusChangedMessage>(this, OrderStatusChangedMessage.Tokens.Orders, OnOrderStatusChangedMessage);
        }

        #endregion

        #region Properties

        public Symbol Symbol { get; } = new Symbol();

        public int Id
        {
            get => _id;
            set => Set(ref _id, value);
        }

        public double EntryPrice
        {
            get => _entryPrice;
            set
            {
                Set(ref _entryPrice, value);
                DispatcherHelper.InvokeOnUiThread(() => SubmitCommand.RaiseCanExecuteChanged());
            }
        }

        public double InitialStopLossPrice
        {
            get => _initialStopLossPrice;
            set => Set(ref _initialStopLossPrice, value);
        }

        public double PriceIncrement
        {
            get => _priceIncrement;
            set => Set(ref _priceIncrement, value);
        }

        public Direction Direction
        {
            get => _direction;
            set
            {
                Set(ref _direction, value);
                CalculateOrderDetails();
            }
        }

        public ushort Quantity
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

        public OrderStatus Status
        {
            get => _status;
            set
            {
                Set(ref _status, value);
                switch (value)
                {
                    case OrderStatus.PreSubmitted:
                    // fall-through
                    case OrderStatus.Submitted:
                    // fall-through
                    case OrderStatus.Filled:
                    // fall-through
                    case OrderStatus.Cancelled:
                        IsLocked = true;
                        break;
                }
            }
        }

        public int QuantityInterval
        {
            get { return _quantityInterval; }
            set
            {
                Set(ref _quantityInterval, value);
            }
        }

        public bool IsLocked
        {
            get => _isLocked;
            private set => Set(ref _isLocked, value);
        }

        public bool IsBusy
        {
            get => _isBusy;
            private set => Set(ref _isBusy, value);
        }
        
        public string FindCommandCaption
        {
            get => _findCommandCaption;
            set => Set(ref _findCommandCaption, value);
        }

        public bool HasHistory
        {
            get => _hasHistory;
            set => Set(ref _hasHistory, value);
        }        

        public AsyncCommand FindCommand
        {
            get
            {
                return _findCommand ?? (_findCommand = new AsyncCommand(
                    DispatcherHelper,
                    () => FindSymbolAndProcessAsync(),
                    () => CanFindOrder()));
            }
        }

        public AsyncCommand SubmitCommand
        {
            get
            {
                return _submitCommand ?? (_submitCommand = new AsyncCommand(
                    DispatcherHelper, 
                    SubmitOrderAsync, 
                    () => Status == OrderStatus.Pending && EntryPrice > 0 && Quantity > 0));
            }
        }

        #endregion

        #region Methods

        public void ProcessFindCommandResults(Symbol symbol, FindCommandResultsModel results)
        {
            // TODO: Is there a better way of doing this?
            Symbol.IsFound = true;
            Symbol.Code = symbol.Code;
            Symbol.Name = symbol.Name;
            Symbol.LatestPrice = symbol.LatestPrice;            
            Symbol.MinTick = symbol.MinTick;

            _orderCalculationService.SetLatestPrice(Symbol.Code, results.LatestPrice);

            ProcessHistory(results.PriceHistory);
            CalculateOrderDetails();
            DispatcherHelper.InvokeOnUiThread(() => SubmitCommand.RaiseCanExecuteChanged());
        }

        private async Task SubmitOrderAsync()
        {
            var contract = MapOrderToContract();
            contract.LocalSymbol = Symbol.Code;

            var order = GetPrimaryOrder();
            await _orderManager.PlaceNewOrderAsync(contract, order);
            Id = order.OrderId;
        }

        private Order GetPrimaryOrder()
        {
            return new Order
            {
                Action = Direction == Direction.Buy
                    ? BrokerConstants.Actions.Buy
                    : BrokerConstants.Actions.Sell,
                OrderType = BrokerConstants.OrderTypes.Stop,
                AuxPrice = Rounding.ValueAdjustedForMinTick(EntryPrice, Symbol.MinTick),
                TotalQuantity = Quantity,
                Account = _accountId,
                ModelCode = string.Empty,
                Tif = BrokerConstants.TimeInForce.Day
            };
        }

        public async Task AttachStopOrderAsync()
        {
            var stopPrice = Rounding.ValueAdjustedForMinTick(InitialStopLossPrice, Symbol.MinTick);
            var order = new Order
            {
                // Action for a Stop order will be the opposite
                Action = Direction == Direction.Buy
                    ? BrokerConstants.Actions.Sell
                    : BrokerConstants.Actions.Buy,

                OrderType = BrokerConstants.OrderTypes.Stop,
                AuxPrice = stopPrice,
                TotalQuantity = Quantity,
                Account = _accountId,
                ModelCode = string.Empty,
                Tif = BrokerConstants.TimeInForce.GoodTilCancelled,
                Transmit = true
            };

            var contract = MapOrderToContract();
            await _orderManager.PlaceNewOrderAsync(contract, order);
        }

        // TODO: Move this to a shared helper
        public Contract MapOrderToContract()
        {
            var contract = new Contract
            {
                Symbol = Symbol.Code,
                SecType = BrokerConstants.Stock,
                Exchange = BrokerConstants.Routers.Smart,
                PrimaryExch = IbClientRequestHelper.MapExchange(Symbol.Exchange),
                Currency = BrokerConstants.UsCurrency,
                LastTradeDateOrContractMonth = string.Empty,
                Strike = 0,
                Multiplier = string.Empty,
                LocalSymbol = string.Empty
            };

            return contract;
        }

        private async Task FindSymbolAndProcessAsync()
        {
            IsBusy = true;
            FindCommandCaption = FindButtonCaptions.Finding;

            try
            {
                var contract = MapOrderToContract();
                var results = await _findSymbolService.IssueFindSymbolRequestAsync(contract).ConfigureAwait(false);
                if (results.Details == null)
                {
                    return;
                }

                var details = results.Details.FirstOrDefault();
                if (details == null)
                {
                    return;
                }
                
                Symbol.IsFound = true;
                Symbol.LatestPrice = results.LatestPrice;
                Symbol.Name = details.LongName;
                Symbol.MinTick = details.MinTick;                
                ProcessHistory(results.PriceHistory);
                CalculateOrderDetails(results.LatestPrice);
                DispatcherHelper.InvokeOnUiThread(() => SubmitCommand.RaiseCanExecuteChanged());
            }
            finally
            {
                IsBusy = false;
                FindCommandCaption = FindButtonCaptions.Default;
            }
        }

        public void CalculateOrderDetails(double? latestPrice = null)
        {
            var symbol = Symbol.Code;
            if (latestPrice.HasValue)
            {
                _orderCalculationService.SetLatestPrice(symbol, latestPrice.Value);
            }
            
            if (!_orderCalculationService.CanCalculate(symbol))
            {
                return;
            }

            EntryPrice = _orderCalculationService.GetEntryPrice(symbol, Direction);
            InitialStopLossPrice = _orderCalculationService.CalculateInitialStopLoss(symbol, Direction);
            Quantity = _orderCalculationService.GetCalculatedQuantity(symbol, Direction);

            //StandardDeviation = _orderCalculationService.CalculateStandardDeviation(symbol);
        }

        private void ProcessHistory(List<HistoricalDataEventArgs> results)
        {
            if (results != null && results.Any())
            {
                HasHistory = true;

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

                _orderCalculationService.SetHistoricalData(Symbol.Code, bars);
            }
            else
            {
                Log.Debug("No historical data found");
            }
        }

        private bool CanFindOrder()
        {
            return !string.IsNullOrEmpty(Symbol.Code) && !IsLocked;
        }

        private void OnSymbolPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName != nameof(Symbol.Code))
            {
                return;
            }

            DispatcherHelper.InvokeOnUiThread(() => FindCommand.RaiseCanExecuteChanged());
        }

        private void OnOrderStatusChangedMessage(OrderStatusChangedMessage message)
        {
            if (message.Symbol != Symbol.Code)
            {
                return;
            }

            UpdateOrderStatus(message.Message.Status);
        }

        private void UpdateOrderStatus(string status)
        {
            switch (status)
            {
                case BrokerConstants.OrderStatus.PreSubmitted:
                    Status = OrderStatus.PreSubmitted;
                    break;

                case BrokerConstants.OrderStatus.Submitted:
                    Status = OrderStatus.Submitted;
                    break;

                case BrokerConstants.OrderStatus.Cancelled:
                    Status = OrderStatus.Cancelled;
                    break;

                case BrokerConstants.OrderStatus.Filled:
                    Status = OrderStatus.Filled;
                    break;

                default:
                    Log.Warning("Status that isn't handled: {0}", status);
                    break;
            }

            DispatcherHelper.InvokeOnUiThread(() =>
            {
                FindCommand.RaiseCanExecuteChanged();
                SubmitCommand.RaiseCanExecuteChanged();
            });
        }

        #endregion
    }
}
