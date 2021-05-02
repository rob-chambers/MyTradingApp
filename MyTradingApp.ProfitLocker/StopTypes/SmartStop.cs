namespace MyTradingApp.ProfitLocker.StopTypes
{
    public class SmartStop : StopType
    {
        public override string OrderType => "TRAIL";

        public override StopValue GetStopValue(StopLossRule rule, double profitPercentage)
        {
            if (profitPercentage > rule.UpperProfitPercentage)
            {
                // Use a very tight stop
                return new StopValue
                {
                    TrailingPercent = 1
                };
            }

            // Use an equation that produces a parabola curve
            // to move the stop closer the more profit we make
            var height = rule.UpperProfitPercentage - rule.LowerProfitPercentage;
            var divisor = height / 3;  // 3 works for the lower/upper of 10/28

            var scale = rule.Percentage.Value;
            var x = (profitPercentage - scale) / divisor;
            x *= x;
            var y = scale - x;

            return new StopValue
            {
                TrailingPercent = y
            };
        }
    }
}