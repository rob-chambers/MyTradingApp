using AutoFinance.Broker.InteractiveBrokers.Controllers;
using AutoFinance.Broker.InteractiveBrokers.EventArgs;
using GalaSoft.MvvmLight.Messaging;
using IBApi;
using MyTradingApp.Domain;
using MyTradingApp.EventMessages;
using MyTradingApp.Messages;
using ObjectDumper;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MyTradingApp.Services
{
    public class PositionManager : IPositionManager
    {
        private readonly ITwsObjectFactory _twsObjectFactory;
        private readonly List<OpenOrderMessage> _orderMessages = new List<OpenOrderMessage>();

        public PositionManager(ITwsObjectFactory twsObjectFactory)
        {
            _twsObjectFactory = twsObjectFactory;
            _twsObjectFactory.TwsCallbackHandler.OrderStatusEvent += HandleOrderStatus;
        }

        private void HandleOrderStatus(object sender, OrderStatusEventArgs args)
        {
            var orderId = args.OrderId;

            // We only care about the status of the trailing stop orders
            var orders = _orderMessages.Where(o => o.OrderId == orderId && o.Order.OrderType == BrokerConstants.OrderTypes.Trail)
                .ToList();

            foreach (var openOrder in orders)
            {
                LogOrder(openOrder);                
                Messenger.Default.Send(new OrderStatusChangedMessage(openOrder.Contract.Symbol, args));
            }
        }

        private static void LogOrder(OpenOrderMessage openOrder)
        {
            try
            {
                Log.Debug(openOrder.DumpToString("Open Order"));
            }
            catch (Exception)
            {
                // Ignore exception
            }
        }

        public async Task<IEnumerable<OpenOrderEventArgs>> RequestOpenOrdersAsync()
        {
            var list = await _twsObjectFactory.TwsControllerBase.RequestOpenOrders();
            return list;
        }

        public async Task UpdateStopOrderAsync(Contract contract, Order order)
        {
            //var newOrder = GetOrder(order);
            //var newOrderType = BrokerConstants.OrderTypes.Stop;
            //var triggerPrice = 11.30;
            //var newStopPrice = 10.60;

            //newOrder.TriggerPrice = triggerPrice;
            //newOrder.AdjustedOrderType = newOrderType;
            //newOrder.AdjustedStopPrice = newStopPrice;

            //var id = _ibClient.NextOrderId;
            //_ibClient.ClientSocket.placeOrder(id, contract, newOrder);
            //_ibClient.NextOrderId++;

            // I want to call placeOrder with the existing order id so that it get's modified
            //var newOrder = order; //GetOrder(order);
            //newOrder.AuxPrice = newStopPrice;

            // We rely on the order's orderid and parentid being set
            // order.ClientId = BrokerConstants.ClientId;

            Log.Debug("In UpdateStopOrderAsync");

            // Specifically set Transmit flag to ensure we send the order
            order.Transmit = true;

            //_ibClient.ClientSocket.placeOrder(order.OrderId, contract, order);
            var id = await _twsObjectFactory.TwsController.GetNextValidIdAsync();

            // TODO: Change to single client id
            order.ClientId = BrokerConstants.ClientId + 1;
            order.OrderId = id;

            Log.Debug("Updating stop order for {0}.  Order id = {1}", contract.Symbol, id);

            var acknowledged = await _twsObjectFactory.TwsController.PlaceOrderAsync(id, contract, order);
            if (!acknowledged)
            {
                Log.Warning("New order ({0}) not acknowledged", id);
            }
        }

        //private Order GetOrder(Order originalOrder)
        //{
        //    var order = new Order
        //    {
        //        OrderId = originalOrder.PermId,
        //        Action = originalOrder.Action,
        //        OrderType = originalOrder.OrderType,
        //        AuxPrice = originalOrder.AuxPrice,
        //        TotalQuantity = originalOrder.TotalQuantity,
        //        Account = originalOrder.Account,
        //        TrailStopPrice = originalOrder.TrailStopPrice,
        //        TrailingPercent = originalOrder.TrailingPercent,
        //        Transmit = originalOrder.Transmit,
        //        Tif = originalOrder.Tif
        //    };
        //    return order;
        //}
    }
}
