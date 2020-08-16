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
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace MyTradingApp.Core.ViewModels
{
    public class PositionsViewModel : DispatcherViewModel
    {
        private readonly IMarketDataManager _marketDataManager;
        private readonly IAccountManager _accountManager;
        private readonly IPositionManager _positionManager;
        private readonly IContractManager _contractManager;
        private readonly IQueueProcessor _queueProcessor;
        private readonly ITradeRecordingService _tradeRecordingService;
        private readonly Dictionary<string, int> _tickerIds = new Dictionary<string, int>();
        private bool _isLoading;
        private string _statusText;

        public PositionsViewModel(
            IDispatcherHelper dispatcherHelper,
            IMarketDataManager marketDataManager,
            IAccountManager accountManager,
            IPositionManager positionManager,
            IContractManager contractManager,
            IQueueProcessor queueProcessor,
            ITradeRecordingService tradeRecordingService)
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
            _tradeRecordingService = tradeRecordingService;
        }

        public ObservableCollectionNoReset<PositionItem> Positions
        {
            get;
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

            var position = Positions.First(p => p.Symbol.Code == message.Symbol);

            var handler = new LoggingErrorHandler();
            _tradeRecordingService.ExitTradeAsync(position, message).FireAndForgetSafeAsync(handler);

            GetPositionsAsync().FireAndForgetSafeAsync(handler);

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
            _marketDataManager.StopActivePriceStreaming(_tickerIds.Values);
            _tickerIds.Clear();
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

        private void ProcessOpenOrders(IEnumerable<OpenOrderEventArgs> orders)
        {
            if (orders == null)
            {
                return;
            }

            foreach (var order in orders.Where(o => IsStopOrder(o)))
            {
                if (!IsValid(order))
                {
                    continue;
                }

                var position = GetPositionForOrder(order);
                if (position != null)
                {
                    continue;
                }

                ProcessOrder(position, order);
            }
        }

        private static void ProcessOrder(PositionItem position, OpenOrderEventArgs order)
        {
            position.Contract = order.Contract;
            position.Order = order.Order;
        }

        private static bool IsValid(OpenOrderEventArgs order)
        {
            if (order.OrderId == 0)
            {
                Log.Warning(order.Dump("Order without an id"));
                return false;
            }

            return true;
        }

        private PositionItem GetPositionForOrder(OpenOrderEventArgs order)
        {
            var symbol = order.Contract.Symbol;
            var positions = Positions.Where(x => x.Symbol.Code == symbol).ToList();
            if (!positions.Any())
            {
                return null;
            }

            if (positions.Count > 1)
            {
                Log.Warning("Found more than one position for {0} - taking first", symbol);
                DebuggerHelper.BreakIfAttached();
            }

            return positions.First();
        }

        private static bool IsStopOrder(OpenOrderEventArgs order)
        {        
            return order.Order.OrderType == BrokerConstants.OrderTypes.Stop ||
                order.Order.OrderType == BrokerConstants.OrderTypes.Trail;
        }

        private async Task ProcessOpenOrderAsync(OpenOrderEventArgs order)
        {
            if (order.Order.OrderType != BrokerConstants.OrderTypes.Stop && order.Order.OrderType != BrokerConstants.OrderTypes.Trail)
            {
                return;
            }

            if (order.OrderId == 0)
            {
                // This order was not submitted via this app.  As we don't have an ID, we can't manage the position
                Log.Warning(order.Dump("Order without an id"));
                return;
            }

            var symbol = order.Contract.Symbol;
            var positions = Positions.Where(x => x.Symbol.Code == symbol).ToList();
            if (!positions.Any())
            {
                return;
            }

            if (positions.Count > 1)
            {
                Log.Warning("Found more than one position for {0} - taking first", symbol);
                DebuggerHelper.BreakIfAttached();
            }

            var position = positions.First();
            position.Contract = order.Contract;
            position.Order = order.Order;

            // Request contract details for all positions in parallel
            await GetContractDetailsAsync(position).ConfigureAwait(false);
        }

        private async Task GetContractDetailsAsync()
        {
            Log.Debug("Getting contract details for all positions in parallel");
            var sw = Stopwatch.StartNew();

            var getContractDetailTasks = new List<Task<List<ContractDetails>>>();
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

        private async Task GetContractDetailsAsync(PositionItem position)
        {
            Log.Debug("Getting contract details for single position");
            var newContract = MapContractToNewContract(position.Contract);

            var detailsList = await _contractManager.RequestDetailsAsync(newContract).ConfigureAwait(false);
            HandleContractDetails(detailsList);
        }

        private void HandleContractDetails(List<ContractDetails> details)
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
                DebuggerHelper.BreakIfAttached();
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
                
                await IteratePositionsAsync(positions);

                // Get associated stop orders
                StatusText = "Getting associated stop orders";
                var orders = await _positionManager.RequestOpenOrdersAsync().ConfigureAwait(false);

                StatusText = $"Processing stop orders";
                ProcessOpenOrders(orders);

                StatusText = "Getting contract details";
                await GetContractDetailsAsync().ConfigureAwait(false);

                StatusText = "Loading trade information from database";
                await _tradeRecordingService.LoadTradesAsync().ConfigureAwait(false);
            }
            catch
            {
                // TODO: Show error to user
                DebuggerHelper.BreakIfAttached();
                throw;
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task IteratePositionsAsync(IEnumerable<PositionItem> positions)
        {
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

                    await RequestStreamingAsync(symbol, newContract);
                    //positionsStopService.Manage(item);
                }
            }
        }

        public async Task GetPositionForSymbolAsync(string symbol)
        {
            IsLoading = true;

            try
            {
                var item = await RequestPositionForSymbolAsync(symbol);
                if (item == null)
                {
                    return;
                }

                RemoveExistingPositionIfRequired(item);
                InsertPosition(item);
                if (CanStream(item))
                {
                    await StartStreamingAsync(item).ConfigureAwait(false);
                }

                await ProcessStopOrderAsync(symbol).ConfigureAwait(false);
            }
            catch
            {
                // TODO: Show error to user
                DebuggerHelper.BreakIfAttached();
                throw;
            }
            finally
            {
                IsLoading = false;
                LogStatus("Finished");
            }
        }

        private async Task ProcessStopOrderAsync(string symbol)
        {
            LogStatus("Getting associated stop orders");
            var orders = await _positionManager.RequestOpenOrdersAsync();

            var order = orders.SingleOrDefault(o => o.Contract.Symbol == symbol);
            if (order != null)
            {
                LogStatus("Processing stop order");
                await ProcessOpenOrderAsync(order).ConfigureAwait(false);
            }
            else
            {
                Log.Warning("No stop order found for {0}", symbol);
            }
        }

        private async Task StartStreamingAsync(PositionItem item)
        {
            var symbol = item.Symbol.Code;
            Log.Debug("Requesting streaming price for position {0}", symbol);
            var newContract = MapContractToNewContract(item.Contract);
            await RequestStreamingAsync(symbol, newContract);
        }

        private bool CanStream(PositionItem item)
        {
            return item.IsOpen && item.Contract != null;
        }

        private void InsertPosition(PositionItem item)
        {
            Positions.Insert(0, item);
        }

        private void RemoveExistingPositionIfRequired(PositionItem item)
        {
            if (Positions.Contains(item))
            {
                Log.Warning("Position already existed - removing");
                Positions.Remove(item);
            }
        }

        private async Task<PositionItem> RequestPositionForSymbolAsync(string symbol)
        {
            LogStatus($"Requesting positions from TWS for symbol {symbol}");
            var positions = await _accountManager.RequestPositionsAsync();
            var item = positions.SingleOrDefault(p => p.Symbol.Code == symbol);
            if (item == null)
            {
                Log.Warning("No position found");
            }

            return item;
        }

        private async Task RequestStreamingAsync(string symbol, Contract newContract)
        {
            StatusText = $"Starting streaming for {symbol}";
            var retryCount = 0;
            bool hadException;

            do
            {
                try
                {
                    Log.Debug("In {0} for {1}", nameof(RequestStreamingAsync), symbol);
                    hadException = false;
                    var tickerId = await _marketDataManager.RequestStreamingPriceAsync(newContract).ConfigureAwait(false);
                    _tickerIds.Add(symbol, tickerId);
                }
                catch (Exception ex)
                {
                    Log.Error("Handling the following exception by retrying.  Retry count={0}\n{1}", retryCount, ex);
                    hadException = true;
                    retryCount++;
                    await Task.Delay(10);
                }
            } while (hadException && retryCount < 3);
        }

        private void LogStatus(string status)
        {
            Log.Debug(status);
            StatusText = status;
        }
    }
}
