using MyTradingApp.ProfitLocker.StopTypes;

namespace MyTradingApp.ProfitLocker
{
    public class Calculator : ICalculator
    {
        private readonly StopTypeFactory _factory;

        public Calculator() : this(new StopTypeFactory())
        {
        }

        public Calculator(StopTypeFactory factory)
        {
            _factory = factory;
        }

        public StopAdjustment CalculateStopLoss(
            double entryPrice,
            double latestPrice,
            StopRulesCollection stopRules)
        {
            var profit = latestPrice - entryPrice;
            var profitPercentage = profit / entryPrice * 100;
            var rule = stopRules.RuleForPercentage(profitPercentage);

            var adjustment = new StopAdjustment();

            // TODO: Check for null rule
            var stopType = _factory.Create(rule.StopType);

            var stopPrice = stopType.GetStopValue(rule, profitPercentage);

            //if (profitPercentage <= stopLossPercentage)
            //    return latestPrice - latestPrice * stopLossPercentage / 100;

            //// For values between 7 and 10% profit, "float" the stop 
            //if (profitPercentage <= StartLockingPercentage)
            //    return entryPrice;

            adjustment.SubmitOrder = stopPrice != null;
            adjustment.OrderType = stopType.OrderType;
            adjustment.StopPrice = stopPrice;
            return adjustment;
        }

        public double CalculateOrderThreshold(StopRulesCollection stopRules)
        {
            //foreach (var rules in stopRules)
            //{

            //}
            return 0;
        }
    }
}
