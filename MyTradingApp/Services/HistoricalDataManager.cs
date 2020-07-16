using GalaSoft.MvvmLight.Messaging;
using IBApi;
using MyTradingApp.Domain;
using MyTradingApp.Messages;
using MyTradingApp.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Bar = MyTradingApp.Domain.Bar;

namespace MyTradingApp.Services
{
    internal class HistoricalDataManager : DataManager, IHistoricalDataManager
    {
        public const string FullDatePattern = "yyyyMMdd  HH:mm:ss";
        private const int HISTORICAL_ID_BASE = 30000000;        
        private const string YearMonthDayPattern = "yyyyMMdd";
        
        private readonly IBClient _ibClient;
        private readonly Dictionary<string, List<HistoricalDataMessage>> _historicalData = new Dictionary<string, List<HistoricalDataMessage>>();
        private readonly Dictionary<int, string> _tickers = new Dictionary<int, string>();

        public HistoricalDataManager(IBClient ibClient) : base(ibClient)
        {
            _ibClient = ibClient;
        }

        public void AddRequest(Contract contract, string endDateTime, string durationString, string barSizeSetting, string whatToShow, int useRTH, int dateFormat, bool keepUpToDate)
        {            
            var symbol = contract.Symbol;
            if (_historicalData.ContainsKey(symbol))
            {
                _historicalData.Remove(symbol);
            }

            var tickerId = currentTicker + HISTORICAL_ID_BASE;
            _tickers.Add(tickerId, symbol);

            _ibClient.ClientSocket.reqHistoricalData(tickerId, contract, endDateTime, durationString, barSizeSetting, whatToShow, useRTH, 1, keepUpToDate, new List<TagValue>());

            currentTicker++;
        }

        public override void Clear()
        {
            _historicalData.Clear();
        }

        public void HandleMessage(HistoricalDataMessage message)
        {
            if (!_tickers.ContainsKey(message.RequestId))
            {
                return;
            }

            var symbol = _tickers[message.RequestId];
            if (!_historicalData.ContainsKey(symbol))
            {
                _historicalData.Add(symbol, new List<HistoricalDataMessage>());
            }

            var list = _historicalData[symbol];
            list.Add(message);
        }

        public void HandleMessage(HistoricalDataEndMessage message)
        {
            if (!_tickers.ContainsKey(message.RequestId))
            {
                return;
            }

            var symbol = _tickers[message.RequestId];
            Messenger.Default.Send(new HistoricalDataCompletedMessage(symbol, PrepareEventData(symbol)));
        }

        private BarCollection PrepareEventData(string symbol)
        {
            var list = new BarCollection();
            var data = _historicalData[symbol];

            foreach (var item in data.Select(x => new Bar
            {
                Date = DateTime.ParseExact(x.Date, YearMonthDayPattern, new CultureInfo("en-US")),
                Open = x.Open,
                High = x.High,
                Low = x.Low,
                Close = x.Close
            }).OrderByDescending(x => x.Date))
            {
                list.Add(item.Date, item);
            }

            return list;
        }

        public override void NotifyError(int requestId)
        {
        }
    }
}