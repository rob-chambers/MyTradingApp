using IBApi;
using MyTradingApp.Messages;

namespace MyTradingApp.Services
{
    public interface IOrderManager
    {
        void PlaceNewOrder(Contract contract, Order order);

        void HandleOrderStatus(OrderStatusMessage message);
    }
}