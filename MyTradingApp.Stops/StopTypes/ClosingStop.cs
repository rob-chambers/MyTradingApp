namespace MyTradingApp.Stops.StopTypes
{
    public class ClosingStop : Stop
    {
        //public GainStopPair Lower { get; set; }

        //public GainStopPair Upper { get; set; }

        public double InitialTrailPercentage { get; set; } // 7

        public double FinalTrailPercentage { get; set; } = 2; // Does this need to be public?

        public double ProfitTargetPercentage { get; set; } // e.g. 28

        public override StopType Type => StopType.Closing;

        public override void CalculatePrice(Position position, double gainPercentage, double high, double low)
        {
            var trailPercentage = CalcClosingStopValue(gainPercentage);

            if (position.Direction == TradeDirection.Long)
            {
                Price = high - high * trailPercentage / 100D;
            }
            else
            {
                Price = low + low * trailPercentage / 100D;
            }
        }

        private double CalcClosingStopValue(double gain)
        {
            //var multiplier = (Lower.TrailingStopPercentage - Upper.TrailingStopPercentage) / (Upper.GainPercentage - Lower.GainPercentage);
            //var value = Lower.TrailingStopPercentage - (gain - Lower.GainPercentage) * multiplier;

            //return value;

            var multiplier = (InitialTrailPercentage - FinalTrailPercentage) / (ProfitTargetPercentage - InitiateAtGainPercentage.Value);
            var value = InitialTrailPercentage - (gain - InitiateAtGainPercentage.Value) * multiplier;

            return value;
        }
    }
}