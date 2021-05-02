using Xunit;

namespace MyTradingApp.ProfitLocker.Tests
{
    public class StopLossRuleComparerTests
    {
        [Fact]
        public void RulesSortedCorrectly()
        {
            // Arrange
            var b = new StopRulesCollection
            {
                new StopLossRule
                {
                    LowerProfitPercentage = 10,
                    UpperProfitPercentage = 20
                },

                new StopLossRule
                {
                    LowerProfitPercentage = 1,
                    UpperProfitPercentage = 2
                }
            };

            // Act
            b.Sort(new StopLossRuleComparer());

            // Assert
            Assert.Equal(1, b[0].LowerProfitPercentage);
            Assert.Equal(20, b[1].UpperProfitPercentage);
        }
    }
}
