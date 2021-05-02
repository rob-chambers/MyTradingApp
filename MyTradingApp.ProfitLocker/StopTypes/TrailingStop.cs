namespace MyTradingApp.ProfitLocker.StopTypes
{
    public class TrailingStop : StopType
    {
        public override string OrderType => "TRAIL";

        public override StopValue GetStopValue(StopLossRule rule, double percentage)
        {
            return new StopValue
            {
                TrailingPercent = rule.Percentage.Value
            };
        }
    }
}