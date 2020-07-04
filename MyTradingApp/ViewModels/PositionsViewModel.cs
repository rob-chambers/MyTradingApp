using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Messaging;
using MyTradingApp.EventMessages;
using MyTradingApp.Models;
using MyTradingApp.Services;
using MyTradingApp.Utils;
using ObjectDumper;
using Serilog;
using System;
using System.Collections.ObjectModel;
using System.Linq;

namespace MyTradingApp.ViewModels
{
    internal class PositionsViewModel : ViewModelBase
    {
        private readonly IMarketDataManager _marketDataManager;
        private readonly IAccountManager _accountManager;
        private readonly IPositionManager _positionManager;
        private readonly IContractManager _contractManager;

        //private RelayCommand<PositionItem> _tempCommand;

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

            _marketDataManager = marketDataManager;
            _accountManager = accountManager;
            _positionManager = positionManager;
            _contractManager = contractManager;
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
            foreach (var item in Positions)
            {
                _marketDataManager.StopPriceStreaming(item.Symbol.Code);
            }
        }

        private void HandlePositionsMessage(ExistingPositionsMessage message)
        {
            Positions.Clear();
            foreach (var item in message.Positions)
            {
                Positions.Add(item);
                if (item.Quantity != 0 && item.Contract != null)
                {
                    _marketDataManager.RequestStreamingPrice(item.Contract);
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
                return;
            }

            position.Symbol.Name = message.Details.LongName;
            position.ContractDetails = message.Details;
            var dump = position.ContractDetails.DumpToString("Contract Details");
            Log.Debug(dump);
        }

        //public RelayCommand<PositionItem> TempCommand => _tempCommand ?? (_tempCommand = new RelayCommand<PositionItem>(MoveStop));

        private void MoveStop(PositionItem position, double newStopPercentage)
        {
            //var order = GetOrder();
            //_positionManager.UpdateStopOrder(position.Contract, order);
            var order = position.Order;
            if (order != null && position.ContractDetails != null)
            {
                var newStop = position.Symbol.LatestHigh - position.Symbol.LatestHigh * newStopPercentage / 100;
                newStop = Rounding.ValueAdjustedForMinTick(newStop, position.ContractDetails.MinTick);

                if (order.AuxPrice != newStop)
                {
                    order.AuxPrice = newStop;
                    _positionManager.UpdateStopOrder(position.Contract, order);
                }
            }
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
    }
}
