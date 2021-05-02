using System.Collections.Generic;
using System.Linq;

namespace MyTradingApp.ProfitLocker
{
    public class StopRulesCollection : List<StopLossRule>
    {
        public StopLossRule RuleForPercentage(double percentage)
        {
            foreach (var rule in this)
            {
                if (rule.LowerProfitPercentage <= percentage &&
                    percentage <= rule.UpperProfitPercentage)
                {
                    return rule;
                }
            }

            var lastRule = this.LastOrDefault();
            if (percentage > lastRule?.UpperProfitPercentage)
            {
                return lastRule;
            }

            return null;
        }
    }
}

