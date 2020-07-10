﻿using GalaSoft.MvvmLight;
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
using System.Diagnostics;
using System.Linq;

namespace MyTradingApp.ViewModels
{
    internal class PositionsViewModel : ViewModelBase
    {
        private readonly IMarketDataManager _marketDataManager;
        private readonly IAccountManager _accountManager;
        private readonly IPositionManager _positionManager;
        private readonly IContractManager _contractManager;
        //private AsyncCommand<PositionItem> _tempCommand;

        public ObservableCollection<PositionItem> Positions { get; } = new ObservableCollection<PositionItem>();

        public PositionsViewModel(
            IMarketDataManager marketDataManager, 
            IAccountManager accountManager,
            IPositionManager positionManager,
            IContractManager contractManager)
        {            
            Messenger.Default.Register<TickPrice>(this, HandleTickPriceMessage);
            Messenger.Default.Register<ConnectionChangingMessage>(this, HandleConnectionChangingMessage);
            Messenger.Default.Register<ConnectionChangedMessage>(this, HandleConnectionChangedMessage);
            Messenger.Default.Register<ExistingPositionsMessage>(this, HandlePositionsMessage);
            Messenger.Default.Register<OpenOrdersMessage>(this, HandleOpenOrdersMessage);
            Messenger.Default.Register<ContractDetailsEventMessage>(this, HandleContractDetailsEventMessage);
            Messenger.Default.Register<OrderStatusChangedMessage>(this, OnOrderStatusChangedMessage);

            _marketDataManager = marketDataManager;
            _accountManager = accountManager;
            _positionManager = positionManager;
            _contractManager = contractManager;
        }

        private void OnOrderStatusChangedMessage(OrderStatusChangedMessage message)
        {
            if (message.Message.Status != BrokerConstants.OrderStatus.Filled)
            {
                return;
            }

            // Find corresponding order
            var position = Positions.SingleOrDefault(p => p.Order?.OrderId == message.Message.OrderId);
            if (position == null)
            {
                // Could be a stop order for trade entry
                return;
            }

            Log.Information("Order id {0} against existing position [{1}] was filled.", position.Order.OrderId, position.Symbol.Code);
            //Log.Debug(message.Message.DumpToString("Order Status"));

            // Double check the entire position was closed
            if (message.Message.Remaining == 0)
            {
                Log.Debug("Marking position as closed and stopping streaming");
                position.Quantity = 0;
                _marketDataManager.StopPriceStreaming(position.Symbol.Code);
            }
            else
            {
                Log.Warning("There are still {0} shares remaining so the position is still open", message.Message.Remaining);
            }
        }

        private void HandleConnectionChangingMessage(ConnectionChangingMessage message)
        {
            if (message.IsConnecting)
            {
                return;
            }

            StopStreaming();
        }

        private void HandleConnectionChangedMessage(ConnectionChangedMessage message)
        {
            if (!message.IsConnected)
            {
                return;
            }

            _accountManager.RequestPositions();
        }

        private void StopStreaming()
        {
            foreach (var item in Positions.Where(p => p.IsOpen))
            {
                _marketDataManager.StopPriceStreaming(item.Symbol.Code);
            }
        }

        private void HandlePositionsMessage(ExistingPositionsMessage message)
        {
            StopStreaming();
            Positions.Clear();
            foreach (var item in message.Positions)
            {
                Positions.Add(item);
                if (item.IsOpen && item.Contract != null)
                {
                    Log.Debug("Requesting streaming price for position {0}", item.Contract.Symbol);
                    //                    ModifyContractForRequest(item.Contract);                    
                    var newContract = MapContractToNewContract(item.Contract);
                    _marketDataManager.RequestStreamingPrice(newContract);
                }
            }

            // Get associated stop orders
            _positionManager.RequestOpenOrders();
        }

        private void HandleTickPriceMessage(TickPrice tickPrice)
        {
            var position = Positions.SingleOrDefault(p => p.Symbol.Code == tickPrice.Symbol);
            if (position == null)
            {
                return;
            }            

            position.Symbol.LatestPrice = tickPrice.Price;
            position.ProfitLoss = position.Quantity * (position.Symbol.LatestPrice - position.AvgPrice);

            position.PercentageGainLoss = Math.Round((position.Symbol.LatestPrice - position.AvgPrice) / position.AvgPrice * 100, 2);
            if (position.Quantity < 0)
            {
                // For shorts, we profit when price falls
                position.PercentageGainLoss = -position.PercentageGainLoss;
            }

            var stop = position.CheckToAdjustStop();
            if (stop.HasValue)
            {
                MoveStop(position, stop.Value);
            }
        }

        private void HandleOpenOrdersMessage(OpenOrdersMessage message)
        {
            foreach (var order in message.Orders.Where(o => o.Order.OrderType == BrokerConstants.OrderTypes.Stop ||
                o.Order.OrderType == BrokerConstants.OrderTypes.Trail))
            {
                if (order.OrderId == 0)
                {
                    // This order was not submitted via this app.  As we don't have an ID, we can't manage the position
                    Log.Warning(message.DumpToString("Order without an id"));
                    continue;
                }

                var symbol = order.Contract.Symbol;
                var position = Positions.SingleOrDefault(x => x.Symbol.Code == symbol);
                if (position != null)
                {
                    position.Contract = order.Contract;
                    position.Order = order.Order;
                }
            }

            // Request contract details for all positions
            foreach (var item in Positions.Where(p => p.Contract != null))
            {
                _contractManager.RequestDetails(item.Contract);
            }
        }

        private void HandleContractDetailsEventMessage(ContractDetailsEventMessage message)
        {
            var symbol = message.Details.Contract.Symbol;
            var position = Positions.SingleOrDefault(p => p.Symbol.Code == symbol);
            if (position == null)
            {
                // This is OK - this event may have been raised for orders
                return;
            }

            position.Symbol.Name = message.Details.LongName;
            position.ContractDetails = message.Details;
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

        private void MoveStop(PositionItem position, double newStopPercentage)
        {
            //var order = GetOrder();
            //_positionManager.UpdateStopOrder(position.Contract, order);
            var order = position.Order;
            if (order != null && position.ContractDetails != null)
            {
                var newStop = position.Quantity > 0
                    ? position.Symbol.LatestHigh - position.Symbol.LatestHigh * newStopPercentage / 100
                    : position.Symbol.LatestLow + position.Symbol.LatestLow * newStopPercentage / 100;
                newStop = Math.Round(newStop, 2);

                if (position.Quantity > 0 && order.AuxPrice < newStop)
                {
                    Log.Debug("Moving stop on {0} from {1} to {2}", position.Symbol.Code, order.AuxPrice, newStop);
                    order.AuxPrice = newStop;
                    _positionManager.UpdateStopOrder(position.Contract, order);
                }
                else if (position.Quantity < 0 && order.AuxPrice > newStop)
                {
                    Log.Debug("Moving stop on {0} from {1} to {2}", position.Symbol.Code, order.AuxPrice, newStop);
                    order.AuxPrice = newStop;
                    _positionManager.UpdateStopOrder(position.Contract, order);
                }
            }
        }

        private static void ModifyContractForRequest(Contract contract)
        {
            contract.PrimaryExch = Exchange.NYSE.ToString();
            contract.Exchange = BrokerConstants.Routers.Smart;
        }

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
            var exchange = originalContract.Exchange;
            if (!Enum.TryParse<Exchange>(exchange, true, out var exchangeEnum))
            {
                Log.Warning("Couldn't find exchange enum value {0}", exchange);
                if (Debugger.IsAttached)
                {
                    Debugger.Break();
                }

                return null;
            }

            var contract = new Contract
            {
                Symbol = originalContract.Symbol,
                SecType = BrokerConstants.Stock,
                Exchange = BrokerConstants.Routers.Smart,
                PrimaryExch = IbClientRequestHelper.MapExchange(exchangeEnum),
                Currency = BrokerConstants.UsCurrency,
                LastTradeDateOrContractMonth = string.Empty,
                Strike = 0,
                Multiplier = string.Empty,
                LocalSymbol = string.Empty
            };

            return contract;
        }
    }
}
