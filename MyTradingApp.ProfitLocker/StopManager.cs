using System;

namespace MyTradingApp.ProfitLocker
{
    public class StopManager
    {
        public const double ProfitChangeTriggerThreshold = 0.5;

        private readonly ICalculator _calculator;


        public StopManager(ICalculator calculator)
        {
            _calculator = calculator;
        }

        public StopRulesCollection StopRules { get; set; }

        //public double EntryPrice { get; set; }

        //public double LatestPrice { get; set; }

        public double? CurrentStopLoss { get; set; }

        public StopAdjustment GetStopAdjustment(double entryPrice, double latestPrice)
        {
            //var result = _calculator.CalculateStopLoss(entryPrice, latestPrice, StopRules);

            ////result.OrderType
            ///*
            // */

            //if (ProfitIncreasedSufficiently(result))
            //{
            //    result.SubmitOrder = true;
            //}

            return null;
        }

        private bool ProfitIncreasedSufficiently(StopAdjustment result)
        {
            if (!CurrentStopLoss.HasValue || 
                result.StopPrice.Price > CurrentStopLoss.Value + ProfitChangeTriggerThreshold)
            {
                return true;
            }

            return false;
        }
    }
}
