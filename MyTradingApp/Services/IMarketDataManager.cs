using IBApi;

namespace MyTradingApp.Services
{
    public interface IMarketDataManager
    {
        void RequestStreamingPrice(Contract contract);

        void StopActivePriceStreaming();

        void RequestLatestPrice(Contract contract);
    }
}