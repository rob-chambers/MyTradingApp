using AutoFinance.Broker.InteractiveBrokers.Controllers;
using AutoFinance.Broker.InteractiveBrokers.Wrappers;
using IBApi;
using MyTradingApp.Messages;
using MyTradingApp.Services;
using NSubstitute;

namespace MyTradingApp.Tests
{
    internal class TestMarketDataManager : MarketDataManager
    {
        public TestMarketDataManager() : base(new IBClient(new EReaderMonitorSignal()), GetTwsObjectFactory())
        {
        }

        private static ITwsObjectFactory GetTwsObjectFactory()
        {
            var factory = Substitute.For<ITwsObjectFactory>();
            factory.TwsCallbackHandler.Returns(Substitute.For<ITwsCallbackHandler>());
            return factory;
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
