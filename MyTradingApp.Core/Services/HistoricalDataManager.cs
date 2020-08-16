using AutoFinance.Broker.InteractiveBrokers.Constants;
using AutoFinance.Broker.InteractiveBrokers.Controllers;
using AutoFinance.Broker.InteractiveBrokers.EventArgs;
using IBApi;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MyTradingApp.Core.Services
{
    public class HistoricalDataManager : IHistoricalDataManager
    {
        public const string FullDatePattern = "yyyyMMdd  HH:mm:ss";

        private readonly ITwsObjectFactory _twsObjectFactory;

        public HistoricalDataManager(ITwsObjectFactory twsObjectFactory)
        {
            _twsObjectFactory = twsObjectFactory;
        }

        public Task<List<HistoricalDataEventArgs>> GetHistoricalDataAsync(
            Contract contract, 
            DateTime endDateTime, 
            TwsDuration duration, 
            TwsBarSizeSetting barSizeSetting, 
            TwsHistoricalDataRequestType whatToShow, 
            bool useRegularTradingHours = true, 
            bool formatDate = true)
        {
            return _twsObjectFactory.TwsController.GetHistoricalDataAsync(contract, endDateTime, duration, barSizeSetting, whatToShow, useRegularTradingHours, formatDate);
        }
    }
}