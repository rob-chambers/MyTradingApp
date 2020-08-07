using System;

namespace MyTradingApp.Core.Utils
{
    internal static class Rounding
    {
        public static double ValueAdjustedForMinTick(double value, double minTick)
        {
            var digits = minTick == 0
                ? 2
                : Convert.ToInt32(Math.Log10(1 / minTick));
            return Math.Round(value, digits);
        }
    }
}
