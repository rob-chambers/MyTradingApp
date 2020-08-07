using MyTradingApp.Core.Utils;
using Xunit;

namespace MyTradingApp.Tests
{
    public class RoundingTests
    {
        [Theory]
        [InlineData(10.12345, 0.01, 10.12)]
        [InlineData(10.125, 0.01, 10.12)]
        [InlineData(10.1250001, 0.01, 10.13)]
        [InlineData(4.987, 0.01, 4.99)]
        [InlineData(2.234567, 0.0001, 2.2346)]
        public void RoundingValueAdjustedForMinTickReturnsCorrectValues(double value, double minTick, double expected)
        {
            var result = Rounding.ValueAdjustedForMinTick(value, minTick);
            Assert.Equal(expected, result);
        }

        [Fact]
        public void RoundingValueAdjustedForMinTickOfZeroReturnsDefaultOfOneCent()
        {
            var result = Rounding.ValueAdjustedForMinTick(5.1234, 0);
            Assert.Equal(5.12, result);
        }
    }
}
