using IBApi;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MyTradingApp.Core.Services
{
    public interface IMarketDataManager
    {
        Task<int> RequestStreamingPriceAsync(Contract contract);

        void StopActivePriceStreaming(IEnumerable<int> tickerIds);

        Task<double> RequestLatestPriceAsync(Contract contract);
    }
}