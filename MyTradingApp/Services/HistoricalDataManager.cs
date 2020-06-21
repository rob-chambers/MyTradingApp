using IBApi;
using MyTradingApp.Messages;
using MyTradingApp.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace MyTradingApp.Services
{
    internal class HistoricalDataManager : DataManager, IHistoricalDataManager
    {
        private const int HISTORICAL_ID_BASE = 30000000;
        public const string FullDatePattern = "yyyyMMdd  HH:mm:ss";
        private const string YearMonthDayPattern = "yyyyMMdd";
        private readonly IBClient _ibClient;

        private List<HistoricalDataMessage> _historicalData;

        public event EventHandler<HistoricalDataCompletedEventArgs> HistoricalDataCompleted;

        public HistoricalDataManager(IBClient ibClient) : base(ibClient)
        {
            _ibClient = ibClient;
        }

        public void AddRequest(Contract contract, string endDateTime, string durationString, string barSizeSetting, string whatToShow, int useRTH, int dateFormat, bool keepUpToDate)
        {
            Clear();
            _ibClient.ClientSocket.reqHistoricalData(currentTicker + HISTORICAL_ID_BASE, contract, endDateTime, durationString, barSizeSetting, whatToShow, useRTH, 1, keepUpToDate, new List<TagValue>());
        }

        public override void Clear()
        {
            _historicalData = new List<HistoricalDataMessage>();
        }

        public void HandleMessage(HistoricalDataMessage message)
        {
            _historicalData.Add(message);
        }

        public void HandleMessage(HistoricalDataEndMessage message)
        {
            HistoricalDataCompleted?.Invoke(this, new HistoricalDataCompletedEventArgs(PrepareEventData()));
        }

        private ICollection<Models.Bar> PrepareEventData()
        {
            var list = new List<Models.Bar>();
            list.AddRange(_historicalData.Select(x => new Models.Bar
            {
                Date = DateTime.ParseExact(x.Date, YearMonthDayPattern, new CultureInfo("en-US")),
                Open = x.Open,
                High = x.High,
                Low = x.Low,
                Close = x.Close
            }).OrderByDescending(x => x.Date));

            return list;
        }

        public override void NotifyError(int requestId)
        {
        }
    }
}
