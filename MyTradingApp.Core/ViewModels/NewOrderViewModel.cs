using AutoFinance.Broker.InteractiveBrokers.EventArgs;
using GalaSoft.MvvmLight;
using MyTradingApp.Core.Services;
using MyTradingApp.Core.Utils;
using MyTradingApp.Domain;
using MyTradingApp.Services;
using MyTradingApp.Utils;
using MyTradingApp.ViewModels;
using Serilog;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace MyTradingApp.Core.ViewModels
{
    public class NewOrderViewModel : DispatcherViewModel
    {
        public const string YearMonthDayPattern = "yyyyMMdd";

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
        private bool _canSubmit;
        private readonly IFindSymbolService _findSymbolService;
        private readonly IOrderCalculationService _orderCalculationService;

        public static class FindButtonCaptions
        {
            public const string Default = "Find";
            public const string Finding = "Finding...";
        }

        public NewOrderViewModel(
            IDispatcherHelper dispatcherHelper, 
            IQueueProcessor queueProcessor,
            IFindSymbolService findSymbolService,
            IOrderCalculationService orderCalculationService)
            : base(dispatcherHelper, queueProcessor)
        {
            PopulateDirectionList();
            Symbol.PropertyChanged += OnSymbolPropertyChanged;
            _findSymbolService = findSymbolService;
            _orderCalculationService = orderCalculationService;
        }

        private void OnSymbolPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName != nameof(Symbol.Code))
            {
                return;
            }

            FindCommand.RaiseCanExecuteChanged();
        }

        public Symbol Symbol { get; } = new Symbol();

        public double PriceIncrement
        {
            get => _priceIncrement;
            set => Set(ref _priceIncrement, value);
        }

        public Direction Direction
        {
            get => _direction;
            set => Set(ref _direction, value);
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

        public ObservableCollection<Direction> DirectionList { get; private set; } = new ObservableCollection<Direction>();

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
                return _submitCommand ?? (_submitCommand = new AsyncCommand(DispatcherHelper, SubmitOrderAsync, () => _canSubmit));
            }
        }

        private Task SubmitOrderAsync()
        {
            throw new NotImplementedException();
        }

        private async Task FindSymbolAndProcessAsync()
        {
            IsBusy = true;
            FindCommandCaption = FindButtonCaptions.Finding;

            try
            {
                var results = await _findSymbolService.IssueFindSymbolRequestAsync(this).ConfigureAwait(false);
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

                _orderCalculationService.SetLatestPrice(Symbol.Code, results.LatestPrice);
                _canSubmit = true;

                DispatcherHelper.InvokeOnUiThread(() =>
                {
                    SubmitCommand.RaiseCanExecuteChanged();
                });

                ProcessHistory(results.PriceHistory);
            }
            finally
            {
                IsBusy = false;
                FindCommandCaption = FindButtonCaptions.Default;
            }
        }

        private void ProcessHistory(List<HistoricalDataEventArgs> results)
        {
            if (results.Any())
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
                //CalculateRisk(Symbol.Code);
            }
            else
            {
                Log.Debug("No historical data found");
            }
        }

        private void PopulateDirectionList()
        {
            var values = Enum.GetValues(typeof(Direction));
            foreach (var value in values)
            {
                DirectionList.Add((Direction)value);
            }
        }

        private bool CanFindOrder()
        {
            return !string.IsNullOrEmpty(Symbol.Code) && !IsLocked;
        }
    }
}
