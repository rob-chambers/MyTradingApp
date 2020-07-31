using AutoFinance.Broker.InteractiveBrokers.EventArgs;
using IBApi;
using System.Collections.Generic;

namespace MyTradingApp.Core.ViewModels
{
    internal class FindCommandResultsModel
    {
        public double LatestPrice { get; set; }

        public IList<ContractDetails> Details { get; set; }

        public List<HistoricalDataEventArgs> PriceHistory { get; set; }
    }
}
