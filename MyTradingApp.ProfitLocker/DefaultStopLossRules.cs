namespace MyTradingApp.ProfitLocker
{
    public class DefaultStopLossRules : StopRulesCollection
    {
        public DefaultStopLossRules()
        {
            Add(new StopLossRule
            {
                StopType = StopTypeValue.Trailing,
                LowerProfitPercentage = 0,
                UpperProfitPercentage = 7,
                Percentage = 7
            });

            Add(new StopLossRule
            {
                StopType = StopTypeValue.Floating,
                LowerProfitPercentage = 7,
                UpperProfitPercentage = 10
            });

            Add(new StopLossRule
            {
                StopType = StopTypeValue.Smart,
                LowerProfitPercentage = 10,
                UpperProfitPercentage = 28,
                Percentage = 10
            });
        }
    }
}
