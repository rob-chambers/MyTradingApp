using GalaSoft.MvvmLight.Messaging;
using IBApi;
using MyTradingApp.EventMessages;
using MyTradingApp.Messages;
using System.Collections.Generic;
using System.Diagnostics;

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
        }

        public void HandleOrderStatus(OrderStatusMessage message)
        {
            var orderId = message.OrderId;
            Debug.WriteLine("Order status: {0}, permid: {1}, orderid: {2}", message.Status, message.PermId, orderId);
            if (_orders.ContainsKey(orderId))
            {
                var symbol = _orders[orderId];
                Messenger.Default.Send(new OrderStatusChangedMessage(symbol, message));
            }
        }

        public int PlaceNewOrder(Contract contract, Order order)
        {
            var id = _ibClient.NextOrderId;
            _orders.Add(id, contract.Symbol);
            _ibClient.ClientSocket.placeOrder(id, contract, order);
            _ibClient.NextOrderId++;
            return id;
        }
    }
}