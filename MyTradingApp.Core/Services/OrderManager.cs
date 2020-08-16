using AutoFinance.Broker.InteractiveBrokers.Controllers;
using AutoFinance.Broker.InteractiveBrokers.EventArgs;
using GalaSoft.MvvmLight.Messaging;
using IBApi;
using MyTradingApp.Core.EventMessages;
using MyTradingApp.Domain;
using Serilog;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MyTradingApp.Core.Services
{
    public class OrderManager : IOrderManager
    {
        private readonly ITwsObjectFactory _twsObjectFactory;
        private readonly IQueueProcessor _queueProcessor;
        private readonly Dictionary<int, string> _orders = new Dictionary<int, string>();
        private bool _isEventHandlerRegistered = false;

        public OrderManager(ITwsObjectFactory twsObjectFactory, IQueueProcessor queueProcessor)
        {
            _twsObjectFactory = twsObjectFactory;
            _queueProcessor = queueProcessor;
        }

        public async Task PlaceNewOrderAsync(Contract contract, Order order)
        {
            if (!_isEventHandlerRegistered)
            {
                _twsObjectFactory.TwsCallbackHandler.OrderStatusEvent += HandleOrderStatus;
                _isEventHandlerRegistered = true;
            }

            var id = await _twsObjectFactory.TwsController.GetNextValidIdAsync().ConfigureAwait(false);

            order.ClientId = BrokerConstants.ClientId;
            order.OrderId = id;
            _orders.Add(id, contract.Symbol);
            var acknowledged = await _twsObjectFactory.TwsController.PlaceOrderAsync(id, contract, order).ConfigureAwait(false);
            if (!acknowledged)
            {
                Log.Warning("New order ({0}) not acknowledged", id);
            }
        }

        private void HandleOrderStatus(object sender, OrderStatusEventArgs args)
        {
            var orderId = args.OrderId;
            Log.Debug("Order status: {0}, permid: {1}, orderid: {2}", args.Status, args.PermId, orderId);
            if (_orders.ContainsKey(orderId))
            {
                var symbol = _orders[orderId];
                _queueProcessor.Enqueue(() => Messenger.Default.Send(new OrderStatusChangedMessage(symbol, args), OrderStatusChangedMessage.Tokens.Orders));
            }
        }
    }
}