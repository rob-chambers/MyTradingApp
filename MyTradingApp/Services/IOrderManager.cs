using IBApi;

namespace MyTradingApp.Services
{
    public interface IOrderManager
    {
        void PlaceNewOrder(Contract contract, Order order);
    }
}