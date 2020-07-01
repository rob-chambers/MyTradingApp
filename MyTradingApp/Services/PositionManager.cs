﻿using GalaSoft.MvvmLight.Messaging;
using IBApi;
using MyTradingApp.EventMessages;
using MyTradingApp.Messages;
using MyTradingApp.Models;
using System.Collections.Generic;

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
            _ibClient.ClientSocket.placeOrder(order.OrderId, contract, order);
        }

        private Order GetOrder(Order originalOrder)
        {
            var order = new Order
            {
                OrderId = originalOrder.PermId,
                Action = originalOrder.Action,
                OrderType = originalOrder.OrderType,
                AuxPrice = originalOrder.AuxPrice,
                TotalQuantity = originalOrder.TotalQuantity,
                Account = originalOrder.Account,
                TrailStopPrice = originalOrder.TrailStopPrice,
                TrailingPercent = originalOrder.TrailingPercent,
                Transmit = originalOrder.Transmit,
                Tif = originalOrder.Tif
            };
            return order;
        }
    }
}
