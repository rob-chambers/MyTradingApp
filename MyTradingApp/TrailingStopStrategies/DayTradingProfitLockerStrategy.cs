using System;

namespace MyTradingApp.TrailingStopStrategies
{
    internal class DayTradingProfitLockerStrategy : ProfitLockerStrategy
    {
        public DayTradingProfitLockerStrategy()
        {
            Risk = 7;
            ProfitTarget = 28;
        }

        public override double ResumingTrailingAtProfitTarget => 10;

        public override double CalculateTrailingStopPercentage(double gainPercentage)
        {
            if (gainPercentage <= Risk)
            {
                return Risk;
            }
            else if (gainPercentage > Risk && gainPercentage < ResumingTrailingAtProfitTarget)
            {
                // TODO: Float
            }

            return CalculateTrailingStop(gainPercentage);
        }
        
        private double CalculateTrailingStop(double gainPercentage)
        {
            // TODO: USe sophisticated calculation
            // For now, keep it simple
            var diff = (ProfitTarget - gainPercentage) / 3 + 2;
            return diff;
        }
    }
}
