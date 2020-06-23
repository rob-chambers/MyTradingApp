using GalaSoft.MvvmLight.Messaging;
using IBApi;
using MyTradingApp.EventMessages;
using MyTradingApp.Messages;
using System.Diagnostics;

namespace MyTradingApp.Services
{
    internal class OrderManager : IOrderManager
    {
        private readonly IBClient _ibClient;

        public OrderManager(IBClient ibClient)
        {
            _ibClient = ibClient;
            ibClient.OrderStatus += HandleOrderStatus;
        }

        public void HandleOrderStatus(OrderStatusMessage message)
        {
            Debug.WriteLine("Order status: {0}, permid: {1}", message.Status, message.PermId);

            Messenger.Default.Send(new OrderStatusChangedMessage(message));

            //for (int i = 0; i < liveOrdersGrid.Rows.Count; i++)
            //{
            //    if (liveOrdersGrid[0, i].Value.Equals(statusMessage.PermId))
            //    {
            //        liveOrdersGrid[8, i].Value = statusMessage.Status;
            //        return;
            //    }
            //}
        }

        public int PlaceNewOrder(Contract contract, Order order)
        {
            var id = _ibClient.NextOrderId;
            _ibClient.ClientSocket.placeOrder(id, contract, order);
            _ibClient.NextOrderId++;
            return id;
        }
    }
}