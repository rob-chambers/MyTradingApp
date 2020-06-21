using IBApi;
using MyTradingApp.Messages;
using MyTradingApp.Models;
using System;

namespace MyTradingApp.Services
{
    public interface IHistoricalDataManager
    {
        event EventHandler<HistoricalDataCompletedEventArgs> HistoricalDataCompleted;

        void AddRequest(Contract contract, string endDateTime, string durationString, string barSizeSetting, string whatToShow, int useRTH, int dateFormat, bool keepUpToDate);

        void HandleMessage(HistoricalDataMessage message);

        void HandleMessage(HistoricalDataEndMessage message);
    }
}