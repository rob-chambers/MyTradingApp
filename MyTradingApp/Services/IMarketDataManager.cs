using IBApi;

namespace MyTradingApp.Services
{
    public interface IMarketDataManager
    {
        void AddRequest(Contract contract, string genericTickList);
        void StopActiveRequests();
    }
}
