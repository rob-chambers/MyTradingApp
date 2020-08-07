using AutoFinance.Broker.InteractiveBrokers.Constants;
using AutoFinance.Broker.InteractiveBrokers.EventArgs;
using IBApi;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MyTradingApp.Core.Services
{
    public interface IHistoricalDataManager
    {
        /// <summary>
        /// Gets historical data from TWS.
        /// </summary>
        /// <param name="contract">The contract type</param>
        /// <param name="endDateTime">The end date of the request</param>
        /// <param name="duration">The duration of the request</param>
        /// <param name="barSizeSetting">The bar size to request</param>
        /// <param name="whatToShow">The historical data request type</param>
        /// <param name="useRth">Whether to use regular trading hours</param>
        /// <param name="formatDate">Whether to format date</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        Task<List<HistoricalDataEventArgs>> GetHistoricalDataAsync(
            Contract contract,
            DateTime endDateTime,
            TwsDuration duration,
            TwsBarSizeSetting barSizeSetting,
            TwsHistoricalDataRequestType whatToShow,
            bool useRegularTradingHours = true,
            bool formatDate = true);
    }
}