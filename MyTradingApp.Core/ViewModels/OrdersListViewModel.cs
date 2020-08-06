using GalaSoft.MvvmLight.Messaging;
using MyTradingApp.Core.Utils;
using MyTradingApp.Domain;
using MyTradingApp.EventMessages;
using MyTradingApp.Repositories;
using MyTradingApp.Services;
using MyTradingApp.Utils;
using MyTradingApp.ViewModels;
using Serilog;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace MyTradingApp.Core.ViewModels
{
    public class OrdersListViewModel : DispatcherViewModel
    {
        private readonly INewOrderViewModelFactory _newOrderViewModelFactory;
        private readonly ITradeRepository _tradeRepository;
        private readonly IMarketDataManager _marketDataManager;
        private readonly List<int> _tickerIds = new List<int>();
        private CommandBase<NewOrderViewModel> _deleteCommand;
        private CommandBase _deleteAllCommand;
        private bool _isStreaming;
        private AsyncCommand _startStopStreamingCommand;        

        public static class StreamingButtonCaptions
        {
            public const string StartStreaming = "Start Streaming";
            public const string StopStreaming = "Stop Streaming";
        }

        public OrdersListViewModel(
            IDispatcherHelper dispatcherHelper, 
            IQueueProcessor queueProcessor,
            INewOrderViewModelFactory newOrderViewModelFactory,
            ITradeRepository tradeRepository,
            IMarketDataManager marketDataManager)
            : base(dispatcherHelper, queueProcessor)
        {
            _newOrderViewModelFactory = newOrderViewModelFactory;
            _tradeRepository = tradeRepository;
            _marketDataManager = marketDataManager;
            PopulateDirectionList();
            Messenger.Default.Register<OrderStatusChangedMessage>(this, OrderStatusChangedMessage.Tokens.Orders, OnOrderStatusChangedMessage);
            Messenger.Default.Register<BarPriceMessage>(this, HandleBarPriceMessage);
            Orders = new ObservableCollectionNoReset<NewOrderViewModel>(dispatcherHelper: DispatcherHelper);
        }

        public ObservableCollection<Direction> DirectionList { get; private set; } = new ObservableCollection<Direction>();

        public ObservableCollectionNoReset<NewOrderViewModel> Orders { get; private set; }

        public CommandBase<NewOrderViewModel> DeleteCommand
        {
            get
            {
                return _deleteCommand ?? (_deleteCommand = new CommandBase<NewOrderViewModel>(DispatcherHelper,
                    order =>
                    {
                        if (Orders.Contains(order))
                        {
                            Orders.Remove(order);
                            DispatcherHelper.InvokeOnUiThread(() =>
                            {
                                DeleteAllCommand.RaiseCanExecuteChanged();
                                StartStopStreamingCommand.RaiseCanExecuteChanged();
                            });
                        }
                    },
                    order => CanDelete(order)));
            }
        }

        public CommandBase DeleteAllCommand
        {
            get
            {
                return _deleteAllCommand ?? (_deleteAllCommand = new CommandBase(DispatcherHelper, DeleteAll, () => Orders.Any()));
            }
        }

        public bool IsStreaming
        {
            get => _isStreaming;
            private set
            {
                Set(ref _isStreaming, value);
                //Messenger.Default.Send(new StreamingChangedMessage(value));
                RaisePropertyChanged(nameof(StreamingButtonCaption));
            }
        }

        public string StreamingButtonCaption => IsStreaming 
            ? StreamingButtonCaptions.StopStreaming 
            : StreamingButtonCaptions.StartStreaming;

        public AsyncCommand StartStopStreamingCommand
        {
            get
            {
                return _startStopStreamingCommand ??
                    (_startStopStreamingCommand = new AsyncCommand(DispatcherHelper, StartStopStreamingAsync, CanStartStopStreaming));
            }
        }

        private async Task StartStopStreamingAsync()
        {
            var isStreaming = IsStreaming;
            try
            {
                IsStreaming = !IsStreaming;
                DispatcherHelper.InvokeOnUiThread(() => StartStopStreamingCommand.RaiseCanExecuteChanged());
                if (IsStreaming)
                {
                    await GetMarketDataAsync().ConfigureAwait(false);
                }
                else
                {
                    CancelStreaming();
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error starting/stopping streaming\n{0}", ex);
                IsStreaming = isStreaming;
            }
        }

        private void CancelStreaming()
        {
            _marketDataManager.StopActivePriceStreaming(_tickerIds);
            _tickerIds.Clear();
        }

        private async Task GetMarketDataAsync()
        {
            var tasks = Orders.Select(o => StreamSymbolAsync(o));
            await Task.WhenAll(tasks).ConfigureAwait(false);
        }

        private async Task StreamSymbolAsync(NewOrderViewModel item)
        {
            var contract = item.MapOrderToContract();
            var tickerId = await _marketDataManager.RequestStreamingPriceAsync(contract);
            _tickerIds.Add(tickerId);
        }

        private bool CanStartStopStreaming()
        {
            return !StartStopStreamingCommand.IsExecuting && (IsStreaming || Orders.Any(o => o.Symbol.IsFound));
        }

        private void PopulateDirectionList()
        {
            var values = Enum.GetValues(typeof(Direction));
            foreach (var value in values)
            {
                DirectionList.Add((Direction)value);
            }
        }

        public void AddOrder(Symbol symbol, FindCommandResultsModel results)
        {
            var order = _newOrderViewModelFactory.Create();
            order.ProcessFindCommandResults(symbol, results);
            Orders.Add(order);
            DispatcherHelper.InvokeOnUiThread(() => 
            {
                DeleteAllCommand.RaiseCanExecuteChanged();
                StartStopStreamingCommand.RaiseCanExecuteChanged();
            });
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

            if (order.Status != OrderStatus.Filled)
            {
                return;
            }

            Log.Debug("A new order for {0} was filled", order.Symbol.Code);
            var addTradeTask = AddTradeAsync(order, message.Message.AvgFillPrice);

            await addTradeTask;
            //    var stopOrderTask = SubmitStopOrderAsync(order, message.Message);

            //    await Task.WhenAll(addTradeTask, stopOrderTask).ConfigureAwait(false);

            //    // This order can be removed now that it is dealt with - it will be added as a position
            //    DispatcherHelper.InvokeOnUiThread(() => Orders.Remove(order));

            //    // Pass this message on to the positions vm now that we have a stop order 
            //    Messenger.Default.Send(message, OrderStatusChangedMessage.Tokens.Positions);
            //}
        }

        private Task AddTradeAsync(NewOrderViewModel order, double fillPrice)
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

        private void DeleteAll()
        {
            foreach (var order in Orders.Where(o => CanDelete(o)).ToList())
            {
                Orders.Remove(order);
            }

            DispatcherHelper.InvokeOnUiThread(() => 
            {
                DeleteAllCommand.RaiseCanExecuteChanged();
                StartStopStreamingCommand.RaiseCanExecuteChanged();
            });
        }

        private bool CanDelete(NewOrderViewModel order)
        {
            return Orders.Contains(order) && (order?.Status == OrderStatus.Pending || order?.Status == OrderStatus.Cancelled);
        }

        private void HandleBarPriceMessage(BarPriceMessage message)
        {
            if (!IsStreaming)
            {
                // It wasn't us that triggered the event
                return;
            }

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
            order.CalculateOrderDetails(message.Bar.Close);
        }
    }
}
