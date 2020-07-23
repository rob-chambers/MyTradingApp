using AutoFinance.Broker.InteractiveBrokers.Constants;
using AutoFinance.Broker.InteractiveBrokers.Controllers;
using AutoFinance.Broker.InteractiveBrokers.EventArgs;
using IBApi;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MyTradingApp.Services
{
    public class HistoricalDataManager : IHistoricalDataManager
    {
        public const string FullDatePattern = "yyyyMMdd  HH:mm:ss";
        
        private readonly ITwsObjectFactory _twsObjectFactory;

        public HistoricalDataManager(ITwsObjectFactory twsObjectFactory)
        {
            _twsObjectFactory = twsObjectFactory;
        }

        public async Task<List<HistoricalDataEventArgs>> GetHistoricalDataAsync(Contract contract, DateTime endDateTime, TwsDuration duration, TwsBarSizeSetting barSizeSetting, TwsHistoricalDataRequestType whatToShow, bool useRegularTradingHours = true, bool formatDate = true)
        {
            var historicalDataEvents = await _twsObjectFactory.TwsController.GetHistoricalDataAsync(contract, endDateTime, TwsDuration.OneMonth, TwsBarSizeSetting.OneDay, TwsHistoricalDataRequestType.Midpoint);
            return historicalDataEvents;
        }
    }
}