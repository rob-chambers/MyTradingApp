using IBApi;
using MyTradingApp.Messages;
using MyTradingApp.Services;

namespace MyTradingApp.Tests
{
    internal class TestMarketDataManager : MarketDataManager
    {
        public TestMarketDataManager() : base(new IBClient(new EReaderMonitorSignal()))
        {
        }

        protected override void RequestMarketData(Contract contract, int nextRequestId)
        {
            // Override base to avoid calling into IB client
        }

        public void RaiseTickPriceMessage(TickPriceMessage message)
        {
            OnTickPrice(message);
        }
    }
}
