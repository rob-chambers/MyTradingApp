using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Messaging;
using IBApi;
using MyTradingApp.EventMessages;
using MyTradingApp.Models;
using MyTradingApp.Services;
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
        private RelayCommand<PositionItem> _tempCommand;

        public ObservableCollection<PositionItem> Positions { get; } = new ObservableCollection<PositionItem>();

        public PositionsViewModel(
            IMarketDataManager marketDataManager, 
            IAccountManager accountManager,
            IPositionManager positionManager)
        {            
            Messenger.Default.Register<TickPrice>(this, HandleTickPriceMessage);
            Messenger.Default.Register<ConnectionChangingMessage>(this, HandleConnectionChangingMessage);
            Messenger.Default.Register<ConnectionChangedMessage>(this, HandleConnectionChangedMessage);
            Messenger.Default.Register<ExistingPositionsMessage>(this, HandlePositionsMessage);
            Messenger.Default.Register<OpenOrdersMessage>(this, HandleOpenOrdersMessage);

            _marketDataManager = marketDataManager;
            _accountManager = accountManager;
            _positionManager = positionManager;
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
                if (item.Quantity > 0)
                {
                    _marketDataManager.RequestStreamingPrice(item.Contract);
                }                
            }

            // Get associated stop orders
            _positionManager.RequestOpenOrders();
        }

        private void HandleTickPriceMessage(TickPrice tickPrice)
        {
            var positon = Positions.SingleOrDefault(p => p.Symbol.Code == tickPrice.Symbol);
            if (positon == null)
            {
                return;
            }            

            positon.Symbol.LatestPrice = tickPrice.Price;
            positon.ProfitLoss = positon.Quantity * (positon.Symbol.LatestPrice - positon.AvgPrice);
            positon.PercentageGainLoss = Math.Round((positon.Symbol.LatestPrice - positon.AvgPrice) / positon.AvgPrice * 100, 2);
        }

        private void HandleOpenOrdersMessage(OpenOrdersMessage message)
        {
            foreach (var order in message.Orders.Where(o => o.Order.OrderType == BrokerConstants.OrderTypes.Stop ||
                o.Order.OrderType == BrokerConstants.OrderTypes.Trail))
            {
                var symbol = order.Contract.Symbol;
                var position = Positions.SingleOrDefault(x => x.Symbol.Code == symbol);
                if (position != null)
                {
                    position.Contract = order.Contract;
                    position.Order = order.Order;
                }
            }
        }

        public RelayCommand<PositionItem> TempCommand => _tempCommand ?? (_tempCommand = new RelayCommand<PositionItem>(MoveStop));

        private void MoveStop(PositionItem position)
        {
            //var order = GetOrder();
            //_positionManager.UpdateStopOrder(position.Contract, order);

        }

        private Order GetOrder()
        {
            // These fields are required when modifying an existing stop
            var order = new Order
            {
                ParentId = 1,
                OrderId = 2,
                Action = BrokerConstants.Actions.Sell,
                OrderType = BrokerConstants.OrderTypes.Stop,
                AuxPrice = 10.70,
                TotalQuantity = 1242
            };

            return order;
        }
    }
}
