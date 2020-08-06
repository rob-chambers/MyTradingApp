using AutoFinance.Broker.InteractiveBrokers.EventArgs;
using GalaSoft.MvvmLight.Messaging;
using IBApi;
using MyTradingApp.Core;
using MyTradingApp.Core.Utils;
using MyTradingApp.Core.ViewModels;
using MyTradingApp.Domain;
using MyTradingApp.EventMessages;
using MyTradingApp.Services;
using MyTradingApp.Utils;
using Serilog;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace MyTradingApp.ViewModels
{
    public class PositionsViewModel : DispatcherViewModel
    {
        private readonly IMarketDataManager _marketDataManager;
        private readonly IAccountManager _accountManager;
        private readonly IPositionManager _positionManager;
        private readonly IContractManager _contractManager;
        private readonly IQueueProcessor _queueProcessor;
        private bool _isLoading;
        private string _statusText;

        //private AsyncCommand<PositionItem> _tempCommand;

        public ObservableCollectionNoReset<PositionItem> Positions { get; }

        public PositionsViewModel(
            IDispatcherHelper dispatcherHelper,
            IMarketDataManager marketDataManager,
            IAccountManager accountManager,
            IPositionManager positionManager,
            IContractManager contractManager,
            IQueueProcessor queueProcessor) 
            : base(dispatcherHelper, queueProcessor)
        {
            Positions = new ObservableCollectionNoReset<PositionItem>(dispatcherHelper: DispatcherHelper);

            Messenger.Default.Register<ConnectionChangingMessage>(this, HandleConnectionChangingMessage);
            Messenger.Default.Register<OrderStatusChangedMessage>(this, OrderStatusChangedMessage.Tokens.Positions, OnOrderStatusChangedMessage);
            Messenger.Default.Register<BarPriceMessage>(this, HandleBarPriceMessage);

            _marketDataManager = marketDataManager;
            _accountManager = accountManager;
            _positionManager = positionManager;
            _contractManager = contractManager;
            _queueProcessor = queueProcessor;
        }

        public bool IsLoading
        {
            get => _isLoading;
            set
            {
                Set(ref _isLoading, value);
            }
        }

        public string StatusText
        {
            get => _statusText;
            set
            {
                Set(ref _statusText, value);
            }
        }

        private void OnOrderStatusChangedMessage(OrderStatusChangedMessage message)
        {
            if (message.Message.Status != BrokerConstants.OrderStatus.Filled)
            {
                return;
            }

            // Find corresponding order
            if (!Positions.Any(p => p.Symbol.Code == message.Symbol))
            {
                // Could be a stop order for trade entry
                return;
            }

            Log.Debug("An order was filled for a position");

            GetPositionsAsync().FireAndForgetSafeAsync(new LoggingErrorHandler());

            //_queueProcessor.Enqueue(async () =>
            //{
            //    Log.Debug("Order status has changed for an existing position, so refreshing all positions.");
            //    await GetPositionsAsync();
            //});

            //Log.Information("Order id {0} against existing position [{1}] was filled.", position.Order.OrderId, position.Symbol.Code);
            ////Log.Debug(message.Message.DumpToString("Order Status"));

            //// Double check the entire position was closed
            //if (message.Message.Remaining == 0)
            //{
            //    Log.Debug("Marking position as closed and stopping streaming");
            //    position.Quantity = 0;
            //    _marketDataManager.StopPriceStreaming(position.Symbol.Code);
            //}
            //else
            //{
            //    Log.Warning("There are still {0} shares remaining so the position is still open", message.Message.Remaining);
            //}
        }

        private void HandleConnectionChangingMessage(ConnectionChangingMessage message)
        {
            if (message.IsConnecting)
            {
                return;
            }

            StopStreaming();
        }

        private void StopStreaming()
        {
            foreach (var item in Positions.Where(p => p.IsOpen))
            {
                _marketDataManager.StopPriceStreaming(item.Symbol.Code);
            }
        }

        private void HandleBarPriceMessage(BarPriceMessage message)
        {
            _queueProcessor.Enqueue(async () =>
            {
                var positions = Positions.Where(p => p.Symbol.Code == message.Symbol && p.IsOpen).ToList();
                if (!positions.Any())
                {
                    return;
                }

                //if (positions.Count > 1)
                //{
                //    Log.Warning("More than one position found for {0}", message.Symbol);
                //}

                var position = positions.First();

                //Log.Debug(message.Bar.DumpToString("{0} Bar"), message.Symbol);
                position.Symbol.LatestPrice = message.Bar.Close;
                position.ProfitLoss = position.Quantity * (position.Symbol.LatestPrice - position.AvgPrice);

                var value = Math.Round((position.Symbol.LatestPrice - position.AvgPrice) / position.AvgPrice * 100, 2);
                if (position.Quantity < 0)
                {
                    // For shorts, we profit when price falls
                    value = -value;
                }

                position.PercentageGainLoss = value;

                var stop = position.CheckToAdjustStop();
                if (stop.HasValue)
                {
                    await MoveStopAsync(position, stop.Value).ConfigureAwait(false);
                }
            });            
        }

        private async Task ProcessOpenOrdersAsync(IEnumerable<OpenOrderEventArgs> orders)
        {
            foreach (var order in orders.Where(o => o.Order.OrderType == BrokerConstants.OrderTypes.Stop ||
                o.Order.OrderType == BrokerConstants.OrderTypes.Trail))
            {
                if (order.OrderId == 0)
                {
                    // This order was not submitted via this app.  As we don't have an ID, we can't manage the position
                    Log.Warning(order.Dump("Order without an id"));
                    continue;
                }

                var symbol = order.Contract.Symbol;
                var positions = Positions.Where(x => x.Symbol.Code == symbol).ToList();
                if (!positions.Any())
                {
                    continue;
                }

                if (positions.Count > 1)
                {
                    Log.Warning("Found more than one position for {0} - taking first", symbol);
                    if (Debugger.IsAttached)
                    {
                        Debugger.Break();
                    }
                }

                var position = positions.First();
                position.Contract = order.Contract;
                position.Order = order.Order;
            }

            // Request contract details for all positions in parallel

            Log.Debug("Getting contract details for all positions in parallel");
            var sw = Stopwatch.StartNew();

            var getContractDetailTasks = new List<Task<IList<ContractDetails>>>();
            foreach (var item in Positions.Where(p => p.Contract != null))
            {
                var newContract = MapContractToNewContract(item.Contract);
                getContractDetailTasks.Add(_contractManager.RequestDetailsAsync(newContract));                
            }

            var detailsList = await Task.WhenAll(getContractDetailTasks).ConfigureAwait(false);
            foreach (var item in detailsList)
            {
                HandleContractDetails(item);
            }

            Log.Debug("Completed getting contract details in {0}ms", sw.ElapsedMilliseconds);
        }

        private void HandleContractDetails(IList<ContractDetails> details)
        {
            if (!details.Any())
            {
                Log.Warning("In Positions VM - No contract details returned");
                return;
            }

            if (details.Count > 1)
            {
                Log.Warning("In Positions VM - Found multiple contract detail items - taking the first");
            }

            var detail = details.First();
            var symbol = detail.Contract.Symbol;
            var positions = Positions.Where(p => p.Symbol.Code == symbol).ToList();
            if (!positions.Any())
            {
                Log.Warning("No positions found matching {0}", symbol);
                return;
            }

            if (positions.Count > 1)
            {
                Log.Warning("Found more than one position for {0} - taking the first", symbol);
            }

            var position = positions.First();
            position.Symbol.Name = detail.LongName;
            position.ContractDetails = detail;
        }

        //public AsyncCommand<PositionItem> TempCommand => _tempCommand ?? (_tempCommand = new AsyncCommand<PositionItem>(DoTempCommand));

        //private async Task DoTempCommand(PositionItem position)
        //{
        //    var result = await Task.Delay(5000).ContinueWith(t =>
        //    {
        //        var r = new Random();
        //        return r.Next(int.MaxValue);
        //    });

        //    position.AvgPrice = result;
        //}

        private async Task MoveStopAsync(PositionItem position, double newStopPercentage)
        {
            //var order = GetOrder();
            //_positionManager.UpdateStopOrder(position.Contract, order);
            var order = position.Order;
            if (order != null && position.ContractDetails != null)
            {
                //var newStop = position.Quantity > 0
                //    ? position.Symbol.LatestHigh - position.Symbol.LatestHigh * newStopPercentage / 100
                //    : position.Symbol.LatestLow + position.Symbol.LatestLow * newStopPercentage / 100;
                //newStop = Math.Round(newStop, 2);

                if (newStopPercentage < order.TrailingPercent)
                {
                    Log.Debug("Tightening trailing stop on {0} from {1}% to {2}%", position.Symbol.Code, order.TrailingPercent, newStopPercentage);
                    order.TrailingPercent = newStopPercentage;
                    await _positionManager.UpdateStopOrderAsync(MapContractToNewContract(position.Contract), order);
                }
            }
        }

        //private static void ModifyContractForRequest(Contract contract)
        //{
        //    contract.PrimaryExch = Exchange.NYSE.ToString();
        //    contract.Exchange = BrokerConstants.Routers.Smart;
        //}

        //private Order GetOrder()
        //{
        //    // These fields are required when modifying an existing stop
        //    var order = new Order
        //    {
        //        ParentId = 1,
        //        OrderId = 2,
        //        Action = BrokerConstants.Actions.Sell,
        //        OrderType = BrokerConstants.OrderTypes.Stop,
        //        AuxPrice = 10.70,
        //        TotalQuantity = 1242
        //    };

        //    return order;
        //}

        private static Contract MapContractToNewContract(Contract originalContract)
        {
            Log.Debug("Mapping contract to new contract.  Symbol={0}, Exchange={1}, Primary Exchange={2}",
                originalContract.Symbol, originalContract.Exchange, originalContract.PrimaryExch);

            var primaryExchange = MapPrimaryExchange(originalContract.Exchange);
            var contract = new Contract
            {
                Symbol = originalContract.Symbol,
                SecType = BrokerConstants.Stock,
                Exchange = BrokerConstants.Routers.Smart,
                PrimaryExch = primaryExchange,
                Currency = BrokerConstants.UsCurrency,
                LastTradeDateOrContractMonth = string.Empty,
                Strike = 0,
                Multiplier = string.Empty,
                LocalSymbol = string.Empty
            };

            return contract;
        }

        private static string MapPrimaryExchange(string exchange)
        {            
            if (exchange == null || exchange.Equals(BrokerConstants.Routers.Smart, StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            if (!Enum.TryParse<Exchange>(exchange, true, out var exchangeEnum))
            {
                Log.Warning("Couldn't find exchange enum value {0}", exchange);
                if (Debugger.IsAttached)
                {
                    Debugger.Break();
                }

                return null;
            }

            return IbClientRequestHelper.MapExchange(exchangeEnum);
        }

        //public async void GetPositions()
        //{
        //    StatusText = "Requesting positions from API";
        //    var positions = await _accountManager.RequestPositionsAsync();

        //    StatusText = "Stopping streaming";
        //    StopStreaming();
        //    DispatchOnUi(() => Positions.Clear());
        //    foreach (var item in positions)
        //    {
        //        DispatchOnUi(() => Positions.Add(item));
        //        if (item.IsOpen && item.Contract != null)
        //        {
        //            var symbol = item.Contract.Symbol;
        //            Log.Debug("Requesting streaming price for position {0}", symbol);
        //            //                    ModifyContractForRequest(item.Contract);                    
        //            var newContract = MapContractToNewContract(item.Contract);

        //            StatusText = $"Starting streaming for {symbol}";
        //            await _marketDataManager.RequestStreamingPriceAsync(newContract);

        //            //positionsStopService.Manage(item);
        //        }
        //    }

        //    // Get associated stop orders

        //    StatusText = "Getting associated stop orders";
        //    var orders = (await _positionManager.RequestOpenOrdersAsync()).ToList();

        //    StatusText = $"Processing stop orders";
        //    await ProcessOpenOrdersAsync(orders);
        //}

        public async Task GetPositionsAsync()
        {
            IsLoading = true;

            try
            {
                StatusText = "Requesting positions from API";
                var positions = await _accountManager.RequestPositionsAsync();
                StatusText = "Stopping streaming";
                StopStreaming();
                Positions.Clear();

                foreach (var item in positions)
                {
                    Positions.Add(item);
                    if (item.IsOpen && item.Contract != null)
                    {
                        var symbol = item.Contract.Symbol;
                        Log.Debug("Requesting streaming price for position {0}", symbol);
                        //                    ModifyContractForRequest(item.Contract);                    
                        var newContract = MapContractToNewContract(item.Contract);

                        StatusText = $"Starting streaming for {symbol}";
                        await _marketDataManager.RequestStreamingPriceAsync(newContract);

                        //positionsStopService.Manage(item);
                    }
                }

                // Get associated stop orders
                StatusText = "Getting associated stop orders";
                var orders = (await _positionManager.RequestOpenOrdersAsync()).ToList();

                StatusText = $"Processing stop orders";
                await ProcessOpenOrdersAsync(orders).ConfigureAwait(false);
            }
            catch
            {
                // TODO: Show error to user
                throw;
            }
            finally
            {
                IsLoading = false;
            }
        }
    }
}
