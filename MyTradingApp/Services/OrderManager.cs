using IBApi;
using MyTradingApp.Messages;
using System.Collections.Generic;
using System.Diagnostics;

namespace MyTradingApp.Services
{
    internal class OrderManager : IOrderManager
    {
        private readonly IBClient _ibClient;

        public OrderManager(IBClient ibClient)
        {
            _ibClient = ibClient;
        }

        public List<string> ManagedAccounts { get; set; }

        public void HandleOrderStatus(OrderStatusMessage message)
        {
            Debug.WriteLine("Order status: {0}, permid: {1}", message.Status, message.PermId);

            //for (int i = 0; i < liveOrdersGrid.Rows.Count; i++)
            //{
            //    if (liveOrdersGrid[0, i].Value.Equals(statusMessage.PermId))
            //    {
            //        liveOrdersGrid[8, i].Value = statusMessage.Status;
            //        return;
            //    }
            //}
        }

        public void PlaceOrder(Contract contract, Order order)
        {
            if (order.OrderId != 0)
            {
                _ibClient.ClientSocket.placeOrder(order.OrderId, contract, order);
            }
            else
            {
                _ibClient.ClientSocket.placeOrder(_ibClient.NextOrderId, contract, order);
                _ibClient.NextOrderId++;
            }
        }
    }
}
