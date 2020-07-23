using IBApi;
using System.Threading.Tasks;

namespace MyTradingApp.Services
{
    public interface IMarketDataManager
    {
        void RequestStreamingPrice(Contract contract, bool ohlc = false);

        void StopActivePriceStreaming();

        Task<double> RequestLatestPriceAsync(Contract contract);

        void StopPriceStreaming(string symbol);
    }
}