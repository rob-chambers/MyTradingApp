//using AutoFinance.Broker.InteractiveBrokers.Controllers;
//using AutoFinance.Broker.InteractiveBrokers.EventArgs;
//using AutoFinance.Broker.InteractiveBrokers.Wrappers;
//using GalaSoft.MvvmLight.Messaging;
//using IBApi;
//using MyTradingApp.Domain;
//using MyTradingApp.EventMessages;
//using MyTradingApp.Messages;
//using MyTradingApp.Services;
//using NSubstitute;
//using System.Collections.Generic;
//using System.Threading.Tasks;
//using System.Windows.Documents;
//using Xunit;

//namespace MyTradingApp.Tests
//{
//    public class MarketDataManagerTests
//    {
//        private TestMarketDataManager GetManager()
//        {
//            return new TestMarketDataManager(GetTwsObjectFactory());
//        }

//        private static ITwsObjectFactory GetTwsObjectFactory()
//        {
//            var factory = Substitute.For<ITwsObjectFactory>();
//            factory.TwsControllerBase.Returns(Substitute.For<ITwsControllerBase>());
//            factory.TwsCallbackHandler.Returns(Substitute.For<ITwsCallbackHandler>());
//            return factory;
//        }

//        [Fact]
//        public async Task WhenLastTickPriceFiredThenSendTickPriceMessage()
//        {
//            // Arrange
//            const string Symbol = "AMZN";
//            const double Price = 11;

//            var fired = false;
//            Messenger.Default.Register<TickPrice>(this, x =>
//            {
//                if (x.Symbol == Symbol &&
//                    x.Price == Price &&
//                    x.Type == TickType.LAST)
//                {
//                    fired = true;
//                }
//            });
//            var manager = GetManager();
//            var contract = new Contract
//            {
//                Symbol = "AMZN",
//                Exchange = BrokerConstants.Routers.Smart,
//                PrimaryExch = Exchange.NYSE.ToString()
//            };

//            manager.ControllerBase.RequestMarketDataAsync(contract, string.Empty, false, false, null)
//                .Returns(Task.FromResult(new TickSnapshotEndEventArgs(123)));


//            // Act
//            await manager.RequestStreamingPriceAsync(contract);
//            //manager.RaiseTickPriceMessage(new TickPriceMessage(MarketDataManager.TICK_ID_BASE + 1, TickType.LAST, Price, new TickAttrib()));

//            // Assert
//            Assert.True(fired);
//        }

//        [Fact]
//        public void WhenPriceIsNegativeThenIgnore()
//        {
//            // Arrange
//            const double Price = -1;
//            var fired = false;
//            Messenger.Default.Register<TickPrice>(this, x => fired = true);

//            var manager = GetManager();

//            // Act
//            manager.RaiseTickPriceMessage(new TickPriceMessage(MarketDataManager.TICK_ID_BASE + 1, TickType.LAST, Price, new TickAttrib()));

//            // Assert
//            Assert.False(fired);
//        }

//        [Fact]
//        public async Task WhenMessageForRequestNotFoundThenIgnore()
//        {
//            // Arrange
//            const double Price = 10;
//            var fired = false;
//            Messenger.Default.Register<TickPrice>(this, x => fired = true);

//            var manager = GetManager();

//            // Act
//            await manager.RequestStreamingPriceAsync(new Contract());
//            manager.RaiseTickPriceMessage(new TickPriceMessage(-1, TickType.LAST, Price, new TickAttrib()));

//            // Assert
//            Assert.False(fired);
//        }

//        [Fact]
//        public async Task WhenRequestForOhlcThenSendBarMessage()
//        {
//            // Arrange
//            const double Open = 10;
//            const double High = 12;
//            const double Low = 9;
//            const double Close = 10.50;

//            var eventCount = 0;

//            var fired = false;
//            Messenger.Default.Register<BarPriceMessage>(this, x =>
//            {
//                eventCount++;
//                if (x.Bar.High == High &&
//                    x.Bar.Low == Low &&
//                    x.Bar.Open == Open &&
//                    x.Bar.Close == Close)
//                {
//                    fired = true;
//                }
//            });

//            var manager = GetManager();

//            // Act
//            await manager.RequestStreamingPriceAsync(new Contract { Symbol = "AMZN" });
//            var requestId = MarketDataManager.TICK_ID_BASE + 1;
//            manager.RaiseTickPriceMessage(new TickPriceMessage(requestId, TickType.OPEN, Open, new TickAttrib()));
//            manager.RaiseTickPriceMessage(new TickPriceMessage(requestId, TickType.HIGH, High, new TickAttrib()));
//            manager.RaiseTickPriceMessage(new TickPriceMessage(requestId, TickType.LOW, Low, new TickAttrib()));
//            manager.RaiseTickPriceMessage(new TickPriceMessage(requestId, TickType.CLOSE, Close, new TickAttrib()));

//            // Assert
//            Assert.True(fired);
//            Assert.Equal(1, eventCount);
//        }
//    }
//}
