using IBApi;
using MyTradingApp.Messages;

namespace MyTradingApp.Services
{
    public interface IHistoricalDataManager
    {
        void AddRequest(Contract contract, string endDateTime, string durationString, string barSizeSetting, string whatToShow, int useRTH, int dateFormat, bool keepUpToDate);

        void HandleMessage(HistoricalDataMessage message);

        void HandleMessage(HistoricalDataEndMessage message);
    }
}