using AutoFinance.Broker.InteractiveBrokers.Constants;
using IBApi;
using MyTradingApp.Core.ViewModels;
using System;
using System.Threading.Tasks;

namespace MyTradingApp.Core.Services
{
    public class FindSymbolService : IFindSymbolService
    {
        private readonly IMarketDataManager _marketDataManager;
        private readonly IHistoricalDataManager _historicalDataManager;
        private readonly IContractManager _contractManager;

        public FindSymbolService(
            IMarketDataManager marketDataManager,
            IHistoricalDataManager historicalDataManager,
            IContractManager contractManager)
        {
            _marketDataManager = marketDataManager;
            _historicalDataManager = historicalDataManager;
            _contractManager = contractManager;
        }

        public async Task<FindCommandResultsModel> IssueFindSymbolRequestAsync(Contract contract)
        {
            var model = new FindCommandResultsModel();
            
            var getLatestPriceTask = _marketDataManager.RequestLatestPriceAsync(contract);
            var getHistoryTask = _historicalDataManager.GetHistoricalDataAsync(
                contract, DateTime.UtcNow, TwsDuration.OneMonth, TwsBarSizeSetting.OneDay, TwsHistoricalDataRequestType.Midpoint);
            var detailsTask = _contractManager.RequestDetailsAsync(contract);

            await Task.WhenAll(getLatestPriceTask, getHistoryTask, detailsTask).ConfigureAwait(false);

            model.LatestPrice = await getLatestPriceTask;
            model.PriceHistory = await getHistoryTask;
            model.Details = await detailsTask;

            return model;
        }
    }
}
