using IBApi;
using System.Threading.Tasks;

namespace MyTradingApp.Services
{
    public interface IMarketDataManager
    {
        Task<int> RequestStreamingPriceAsync(Contract contract);

        void StopActivePriceStreaming();

        Task<double> RequestLatestPriceAsync(Contract contract);

        void StopPriceStreaming(string symbol);
    }
}