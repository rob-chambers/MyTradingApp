using GalaSoft.MvvmLight.Messaging;
using IBApi;
using MyTradingApp.Domain;
using MyTradingApp.EventMessages;
using MyTradingApp.Messages;
using MyTradingApp.Models;
using ObjectDumper;
using Serilog;
using System.Collections.Generic;

namespace MyTradingApp.Services
{
    internal class OrderManager : IOrderManager
    {
        private readonly IBClient _ibClient;
        private readonly Dictionary<int, string> _orders = new Dictionary<int, string>();

        public OrderManager(IBClient ibClient)
        {
            _ibClient = ibClient;
            ibClient.OrderStatus += HandleOrderStatus;
            ibClient.CompletedOrder += HandleCompletedOrder;
            ibClient.CompletedOrdersEnd += HandleCompletedOrdersEnd;
        }

        private void HandleCompletedOrdersEnd()
        {
            Log.Debug("End of completed orders");
        }

        private void HandleCompletedOrder(CompletedOrderMessage message)
        {
            var value = message.DumpToString("Completed Order Message");
            Log.Debug(value);
        }

        private void HandleOrderStatus(OrderStatusMessage message)
        {
            var orderId = message.OrderId;
            Log.Debug("Order status: {0}, permid: {1}, orderid: {2}", message.Status, message.PermId, orderId);
            if (_orders.ContainsKey(orderId))
            {
                var symbol = _orders[orderId];
                Messenger.Default.Send(new OrderStatusChangedMessage(symbol, message));
            }
        }

        public void PlaceNewOrder(Contract contract, Order order)
        {
            var id = _ibClient.NextOrderId;
            order.ClientId = BrokerConstants.ClientId;
            _orders.Add(id, contract.Symbol);
            order.OrderId = id;
            _ibClient.ClientSocket.placeOrder(id, contract, order);
            _ibClient.NextOrderId++;
         }
    }
}