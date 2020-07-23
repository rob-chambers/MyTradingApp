using AutoFinance.Broker.InteractiveBrokers.Controllers;
using AutoFinance.Broker.InteractiveBrokers.EventArgs;
using GalaSoft.MvvmLight.Messaging;
using IBApi;
using MyTradingApp.Domain;
using MyTradingApp.EventMessages;
using Serilog;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MyTradingApp.Services
{
    public class OrderManager : IOrderManager
    {
        private readonly ITwsObjectFactory _twsObjectFactory;
        private readonly Dictionary<int, string> _orders = new Dictionary<int, string>();

        public OrderManager(ITwsObjectFactory twsObjectFactory)
        {
            _twsObjectFactory = twsObjectFactory;
            _twsObjectFactory.TwsCallbackHandler.OrderStatusEvent += HandleOrderStatus;
        }

        private void HandleOrderStatus(object sender, OrderStatusEventArgs args)
        {
            var orderId = args.OrderId;
            Log.Debug("Order status: {0}, permid: {1}, orderid: {2}", args.Status, args.PermId, orderId);
            if (_orders.ContainsKey(orderId))
            {
                var symbol = _orders[orderId];
                Messenger.Default.Send(new OrderStatusChangedMessage(symbol, args));
            }
        }

        public async Task PlaceNewOrderAsync(Contract contract, Order order)
        {
            var id = await _twsObjectFactory.TwsController.GetNextValidIdAsync();

            // TODO: Change to single client id
            order.ClientId = BrokerConstants.ClientId + 1;
            order.OrderId = id;
            var acknowledged = await _twsObjectFactory.TwsController.PlaceOrderAsync(id, contract, order);
            if (!acknowledged)
            {
                Log.Warning("New order ({0}) not acknowledged", id);
            }
        }
    }
}