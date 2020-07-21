using IBApi;

namespace MyTradingApp.Services
{
    public interface IPositionManager
    {
        void RequestOpenOrders();
        void UpdateStopOrder(Contract contract, Order order);
    }
}