using MyTradingApp.Stops.StopTypes;
using Xunit;

namespace MyTradingApp.Tests.Stops
{
    public class StopTests
    {
        [Fact]
        public void BothValuesSet()
        {
            var trailingStop = new TrailingStop
            {
                InitiateAtGainPercentage = 7
            };

            var standardStop = new StandardStop
            {
                InitiateAtGainPercentage = 10
            };

            var stops = new StopCollection
            {                
                standardStop,
                trailingStop
            };

            stops.Sort();

            Assert.Equal(trailingStop, stops[0]);
            Assert.Equal(standardStop, stops[1]);
        }

        [Fact]
        public void ComparisionTestWithNull()
        {
            var trailingStop = new TrailingStop
            {
                InitiateAtGainPercentage = 7
            };

            Assert.False(trailingStop.Equals(null));
        }

        [Fact]
        public void ComparisionTestWithSame()
        {
            var trailingStop1 = new TrailingStop
            {
                InitiateAtGainPercentage = 7
            };
            var trailingStop2 = new TrailingStop
            {
                InitiateAtGainPercentage = 7
            };

            Assert.Equal(trailingStop1, trailingStop2);
        }

        [Fact]
        public void ComparisionTestWithDifferent()
        {
            var trailingStop1 = new TrailingStop
            {
                InitiateAtGainPercentage = 7
            };
            var trailingStop2 = new TrailingStop
            {
                InitiateAtGainPercentage = 6
            };

            Assert.NotEqual(trailingStop1, trailingStop2);
        }
    }
}
