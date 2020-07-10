using GalaSoft.MvvmLight.Messaging;
using IBApi;
using MyTradingApp.EventMessages;
using MyTradingApp.Messages;
using MyTradingApp.Models;
using ObjectDumper;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MyTradingApp.Services
{
    internal class PositionManager : IPositionManager
    {
        private readonly IBClient _ibClient;
        private readonly List<OpenOrderMessage> _orderMessages = new List<OpenOrderMessage>();

        public PositionManager(IBClient ibClient)
        {
            _ibClient = ibClient;
            _ibClient.OpenOrder += HandleOpenOrder;
            _ibClient.OpenOrderEnd += HandleOpenOrderEnd;
            _ibClient.OrderStatus += HandleOrderStatus;
        }

        private void HandleOrderStatus(OrderStatusMessage message)
        {
            var orderId = message.OrderId;

            // We only care about the status of stop orders that have a parent (which mean it's not an order to enter)
            var orders = _orderMessages.Where(o => o.OrderId == orderId &&
                o.Order.ParentId > 0 &&
                (o.Order.OrderType == BrokerConstants.OrderTypes.Stop ||
                    o.Order.OrderType == BrokerConstants.OrderTypes.Trail))
                .ToList();

            foreach (var openOrder in orders)
            {
                LogOrder(openOrder);                
                Messenger.Default.Send(new OrderStatusChangedMessage(openOrder.Contract.Symbol, message));
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

        private void HandleOpenOrder(OpenOrderMessage openOrder)
        {
            _orderMessages.Add(openOrder);
        }

        private void HandleOpenOrderEnd()
        {
            Messenger.Default.Send(new OpenOrdersMessage(_orderMessages));
        }

        public void RequestOpenOrders()
        {
            _orderMessages.Clear();
            _ibClient.ClientSocket.reqAllOpenOrders();
        }

        public void UpdateStopOrder(Contract contract, Order order)
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
            order.ClientId = BrokerConstants.ClientId;
            
            // Specifically set Transmit flag to ensure we send the order
            order.Transmit = true;

            _ibClient.ClientSocket.placeOrder(order.OrderId, contract, order);
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
