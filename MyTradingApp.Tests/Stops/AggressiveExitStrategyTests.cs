using MyTradingApp.Stops;
using MyTradingApp.Stops.StopTypes;
using Xunit;

namespace MyTradingApp.Tests.Stops
{
    public class AggressiveExitStrategyTests
    {
        [InlineData(-5)]
        [InlineData(-1)]
        [InlineData(0)]
        [InlineData(5)]
        [InlineData(9.5)]
        [Theory]
        public void WhenNoneOrLittleGainUseTrailingStop(double gain)
        {
            // Arrange
            var strategy = new AggressiveExitStrategy();

            // Act
            var stop = strategy.GetStopForPercentageGain(gain);

            // Assert
            Assert.IsType<TrailingStop>(stop);
        }

        [InlineData(10)]
        [InlineData(11)]
        [InlineData(12.99)]
        [Theory]
        public void WhenSomeGainUseStandardStop(double gain)
        {
            // Arrange
            var strategy = new AggressiveExitStrategy();

            // Act
            var stop = strategy.GetStopForPercentageGain(gain);

            // Assert
            Assert.IsType<StandardStop>(stop);
        }

        [InlineData(13)]
        [InlineData(18)]
        [InlineData(22)]
        [InlineData(28)]
        [InlineData(33)]
        [Theory]
        public void WhenSolidGainUseClosingStop(double gain)
        {
            // Arrange
            var strategy = new AggressiveExitStrategy();

            // Act
            var stop = strategy.GetStopForPercentageGain(gain);

            // Assert
            Assert.IsType<ClosingStop>(stop);
        }
    }
}
