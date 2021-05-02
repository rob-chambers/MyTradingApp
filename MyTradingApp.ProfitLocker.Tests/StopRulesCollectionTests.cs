using Xunit;

namespace MyTradingApp.ProfitLocker.Tests
{
    public class StopRulesCollectionTests
    {
        [Fact]
        public void WhenNoRulesReturnNull()
        {
            var rules = new StopRulesCollection();
            var rule = rules.RuleForPercentage(0);
            Assert.Null(rule);
        }

        [Fact]
        public void WhenOutOfRangeReturnNull()
        {
            var rules = new StopRulesCollection
            {
                new StopLossRule
                {
                    LowerProfitPercentage = 1,
                    UpperProfitPercentage = 10
                }
            };
            var rule = rules.RuleForPercentage(0);
            Assert.Null(rule);
        }

        [Fact]
        public void WhenGreaterThanMaxReturnLastRule()
        {
            var rules = new StopRulesCollection
            {
                new StopLossRule
                {
                    LowerProfitPercentage = 1,
                    UpperProfitPercentage = 10
                }
            };
            var rule = rules.RuleForPercentage(20);
            Assert.NotNull(rule);
        }

        [Theory]
        [InlineData(1)]
        [InlineData(10)]
        public void ForSingleRuleOnEdgeOfRangeReturnRule(double value)
        {
            var rules = new StopRulesCollection
            {
                new StopLossRule
                {
                    LowerProfitPercentage = 1,
                    UpperProfitPercentage = 10
                }
            };
            var rule = rules.RuleForPercentage(value);
            Assert.NotNull(rule);
        }

        [Fact]
        public void ForMultipleRulesOnEdgeOfRangeReturnFirstRule()
        {
            var rules = new StopRulesCollection
            {
                new StopLossRule
                {
                    LowerProfitPercentage = 1,
                    UpperProfitPercentage = 10,
                    StopType = StopTypeValue.Trailing
                },
                new StopLossRule
                {
                    LowerProfitPercentage = 10,
                    UpperProfitPercentage = 13,
                    StopType = StopTypeValue.Floating
                }
            };
            var rule = rules.RuleForPercentage(10);
            Assert.Equal(StopTypeValue.Trailing, rule.StopType);
        }

        [Fact]
        public void ForMultipleRulesWhenGreaterThanMaxReturnLastRule()
        {
            var rules = new StopRulesCollection
            {
                new StopLossRule
                {
                    LowerProfitPercentage = 1,
                    UpperProfitPercentage = 10,
                    StopType = StopTypeValue.Trailing
                },
                new StopLossRule
                {
                    LowerProfitPercentage = 10,
                    UpperProfitPercentage = 20,
                    StopType = StopTypeValue.Smart
                }
            };
            var rule = rules.RuleForPercentage(30);
            Assert.Equal(StopTypeValue.Smart, rule.StopType);
        }
    }
}
