//using AutoFinance.Broker.InteractiveBrokers.Controllers;
//using AutoFinance.Broker.InteractiveBrokers.Wrappers;
//using IBApi;
//using MyTradingApp.Messages;
//using MyTradingApp.Services;

//namespace MyTradingApp.Tests
//{
//    internal class TestMarketDataManager : MarketDataManager
//    {
//        public ITwsControllerBase ControllerBase { get; private set; }
//        public ITwsCallbackHandler CallbackHandler { get; }

//        public TestMarketDataManager(ITwsObjectFactory factory) : base(new IBClient(new EReaderMonitorSignal()), factory)
//        {
//            ControllerBase = factory.TwsControllerBase;
//            CallbackHandler = factory.TwsCallbackHandler;
//        }

//        protected override void RequestMarketData(Contract contract, int nextRequestId)
//        {
//            // Override base to avoid calling into IB client
//        }

//        public void RaiseTickPriceMessage(TickPriceMessage message)
//        {
//            //OnTickPrice(message);
//        }
//    }
//}
