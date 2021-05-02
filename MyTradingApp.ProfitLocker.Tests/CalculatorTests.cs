using System.Collections.Generic;
using Xunit;

namespace MyTradingApp.ProfitLocker.Tests
{
    public class CalculatorTests
    {
        [Theory]
        [InlineData(0)]
        [InlineData(2)]
        public void WhenZeroProfitTrailingStopUsed(double profitPercentage)
        {
            const double EntryPrice = 10;
            const double Percentage = 6.5;

            var calculator = new Calculator();
            var currentPrice = EntryPrice * (1 + profitPercentage/100);
            var rules = new StopRulesCollection
            {
                new StopLossRule
                {
                    StopType = StopTypeValue.Trailing,
                    LowerProfitPercentage = 0,
                    UpperProfitPercentage = 10,
                    Percentage = Percentage
                }
            };
            var result = calculator.CalculateStopLoss(EntryPrice, currentPrice, rules);

            Assert.Equal("TRAIL", result.OrderType);
            Assert.Equal(Percentage, result.StopPrice.TrailingPercent);
        }

        [Fact]
        public void WhenProfitReachesBreakEvenLeaveExistingStopOrder()
        {
            // Arrange
            const double EntryPrice = 100;
            var calculator = new Calculator();
            var currentPrice = EntryPrice + 10;

            // Act
            var result = calculator.CalculateStopLoss(EntryPrice, currentPrice, new DefaultStopLossRules());

            // Assert
            Assert.Equal("STP", result.OrderType);
            Assert.Null(result.StopPrice);
            Assert.False(result.SubmitOrder);
        }

        [Theory]
        [InlineData(13)]
        [InlineData(20)]
        [InlineData(25)]
        [InlineData(27.9)]
        public void WhenTradeInProfitStopCalculatedCorrectly(double profitPercentage)
        {
            // Arrange
            const double EntryPrice = 100;
            var calculator = new Calculator();
            var currentPrice = EntryPrice * (1 + profitPercentage / 100);

            var expectancyTable = new Dictionary<double, double>()
            {
                { 13, 9.750000000000002 },
                { 20, 7.222222222222221  },
                { 25, 3.75 },
                { 27.9, 1.0997222222222298 }
            };

            var expected = expectancyTable[profitPercentage];

            // Act
            var stop = calculator.CalculateStopLoss(EntryPrice, currentPrice, new DefaultStopLossRules());

            // Assert
            Assert.True(stop.SubmitOrder);
            Assert.Equal("TRAIL", stop.OrderType);
            Assert.Equal(expected, stop.StopPrice.TrailingPercent);
        }

        [Fact]
        public void WhenPriceAtTargetUseTightTrail()
        {
            // Arrange
            const double EntryPrice = 100;
            var calculator = new Calculator();
            var currentPrice = EntryPrice + 28;

            // Act
            var stop = calculator.CalculateStopLoss(EntryPrice, currentPrice, new DefaultStopLossRules());

            // Assert
            Assert.True(stop.SubmitOrder);
            Assert.Equal("TRAIL", stop.OrderType);
            Assert.Equal(1, stop.StopPrice.TrailingPercent);
        }

        [Fact]
        public void WhenPriceSlightlyAboveTargetUseOnePercentTrailingStop()
        {
            // Arrange
            const double EntryPrice = 10;
            var calculator = new Calculator();
            var currentPrice = EntryPrice * 1.3; // 30% profit

            // Act
            var result = calculator.CalculateStopLoss(EntryPrice, currentPrice, new DefaultStopLossRules());

            // Assert
            Assert.True(result.SubmitOrder);
            Assert.Equal(1, result.StopPrice.TrailingPercent);
        }
    }
}
