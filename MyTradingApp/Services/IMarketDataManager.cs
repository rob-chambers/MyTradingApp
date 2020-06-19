using IBApi;

namespace MyTradingApp.Services
{
    interface IMarketDataManager
    {
        void AddRequest(Contract contract, string genericTickList);
        void StopActiveRequests();
    }
}
