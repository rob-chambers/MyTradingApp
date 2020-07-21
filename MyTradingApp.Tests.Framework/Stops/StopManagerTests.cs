using MyTradingApp.Domain;
using MyTradingApp.Stops;
using MyTradingApp.Stops.StopTypes;
using System;
using Xunit;

namespace MyTradingApp.Tests.Stops
{
    public class StopManagerTests
    {
        [Fact]
        public void StopPriceSetCorrectlyForLong()
        {
            // Arrange
            const double StopPercentage = 10;
            const double High = 10.20;

            var manager = new StopManager();
            var bars = new BarCollection
            {
                { 
                    DateTime.Today, 
                    new Bar(DateTime.Today, 10, High, 9, 10) 
                }
            };

            manager.Position = new Position
            {
                Direction = Direction.Buy,
                EntryPrice = 10,
                ExitStrategy = new AggressiveExitStrategy()
            };

            manager.SetHistoricalBars(bars);

            // Act
            var stop = manager.GetStop(DateTime.Today);

            // Assert
            Assert.IsType<TrailingStop>(stop);
            Assert.Equal(High - High * StopPercentage / 100, stop.Price);
        }

        [Fact]
        public void StopPriceSetCorrectlyForShort()
        {
            // Arrange
            const double StopPercentage = 10;
            const double Low = 9.80;

            var manager = new StopManager();
            var bars = new BarCollection
            {
                {
                    DateTime.Today,
                    new Bar(DateTime.Today, 10, 10.20, Low, 10)
                }
            };

            manager.Position = new Position
            {
                Direction = Direction.Sell,
                EntryPrice = 10,
                ExitStrategy = new AggressiveExitStrategy()
            };

            manager.SetHistoricalBars(bars);

            // Act
            var stop = manager.GetStop(DateTime.Today);

            // Assert
            Assert.IsType<TrailingStop>(stop);
            Assert.Equal(Low + Low * StopPercentage / 100, stop.Price);
        }

        [Fact]
        public void WhenNoPositionSetHistoricalBarsThrowsException()
        {
            var manager = new StopManager();
            var bars = new BarCollection();            

            Assert.Throws<InvalidOperationException>(() => manager.SetHistoricalBars(bars));
        }

        [Fact]
        public void WhenNoPositionSetGetStopThrowsException()
        {
            var manager = new StopManager();
            var bars = new BarCollection();

            Assert.Throws<InvalidOperationException>(() => manager.GetStop(DateTime.Today));
        }
    }
}
