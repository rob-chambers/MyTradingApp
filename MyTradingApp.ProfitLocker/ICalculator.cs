namespace MyTradingApp.ProfitLocker
{
    public interface ICalculator
    {
        StopAdjustment CalculateStopLoss(
                    double entryPrice,
                    double latestPrice,
                    StopRulesCollection stopRules);
    }
}