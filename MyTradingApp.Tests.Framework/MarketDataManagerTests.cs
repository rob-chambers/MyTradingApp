using GalaSoft.MvvmLight.Messaging;
using IBApi;
using MyTradingApp.Domain;
using MyTradingApp.EventMessages;
using MyTradingApp.Messages;
using MyTradingApp.Services;
using Xunit;

namespace MyTradingApp.Tests
{
    public class MarketDataManagerTests
    {
        [Fact]
        public void WhenLastTickPriceFiredThenSendTickPriceMessage()
        {
            // Arrange
            const string Symbol = "AMZN";
            const double Price = 11;

            var fired = false;
            Messenger.Default.Register<TickPrice>(this, x =>
            {
                if (x.Symbol == Symbol &&
                    x.Price == Price &&
                    x.Type == TickType.LAST)
                {
                    fired = true;
                }
            });
            var manager = new TestMarketDataManager();
            var contract = new Contract
            {
                Symbol = "AMZN",
                Exchange = BrokerConstants.Routers.Smart,
                PrimaryExch = Exchange.NYSE.ToString()
            };

            // Act
            manager.RequestStreamingPrice(contract, false);
            manager.RaiseTickPriceMessage(new TickPriceMessage(MarketDataManager.TICK_ID_BASE + 1, TickType.LAST, Price, new TickAttrib()));

            // Assert
            Assert.True(fired);
        }

        [Fact]
        public void WhenPriceIsNegativeThenIgnore()
        {
            // Arrange
            const double Price = -1;
            var fired = false;
            Messenger.Default.Register<TickPrice>(this, x => fired = true);

            var manager = new TestMarketDataManager();

            // Act
            manager.RaiseTickPriceMessage(new TickPriceMessage(MarketDataManager.TICK_ID_BASE + 1, TickType.LAST, Price, new TickAttrib()));

            // Assert
            Assert.False(fired);
        }

        [Fact]
        public void WhenMessageForRequestNotFoundThenIgnore()
        {
            // Arrange
            const double Price = 10;
            var fired = false;
            Messenger.Default.Register<TickPrice>(this, x => fired = true);

            var manager = new TestMarketDataManager();

            // Act
            manager.RequestStreamingPrice(new Contract(), false);
            manager.RaiseTickPriceMessage(new TickPriceMessage(-1, TickType.LAST, Price, new TickAttrib()));

            // Assert
            Assert.False(fired);
        }

        [Fact]
        public void WhenRequestForOhlcThenSendBarMessage()
        {
            // Arrange
            const double Open = 10;
            const double High = 12;
            const double Low = 9;
            const double Close = 10.50;

            var eventCount = 0;

            var fired = false;
            Messenger.Default.Register<BarPriceMessage>(this, x =>
            {
                eventCount++;
                if (x.Bar.High == High &&
                    x.Bar.Low == Low &&
                    x.Bar.Open == Open &&
                    x.Bar.Close == Close)
                {
                    fired = true;
                }
            });

            var manager = new TestMarketDataManager();

            // Act
            manager.RequestStreamingPrice(new Contract { Symbol = "AMZN" }, true);
            var requestId = MarketDataManager.TICK_ID_BASE + 1;
            manager.RaiseTickPriceMessage(new TickPriceMessage(requestId, TickType.OPEN, Open, new TickAttrib()));
            manager.RaiseTickPriceMessage(new TickPriceMessage(requestId, TickType.HIGH, High, new TickAttrib()));
            manager.RaiseTickPriceMessage(new TickPriceMessage(requestId, TickType.LOW, Low, new TickAttrib()));
            manager.RaiseTickPriceMessage(new TickPriceMessage(requestId, TickType.CLOSE, Close, new TickAttrib()));

            // Assert
            Assert.True(fired);
            Assert.Equal(1, eventCount);
        }
    }
}
