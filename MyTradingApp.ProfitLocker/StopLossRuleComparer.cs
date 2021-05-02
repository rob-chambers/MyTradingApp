using System.Collections.Generic;

namespace MyTradingApp.ProfitLocker
{
    public class StopLossRuleComparer : IComparer<StopLossRule>
    {
        public int Compare(StopLossRule x, StopLossRule y)
        {
            return x.LowerProfitPercentage.CompareTo(y.LowerProfitPercentage);
        }
    }
}
