namespace MyTradingApp.Stops.StopTypes
{
    public class ClosingStop : Stop
    {
        private const double FinalTrailPercentage = 2;

        public override StopType Type => StopType.Closing;

        public double InitialTrailPercentage { get; set; } // 7        

        public double ProfitTargetPercentage { get; set; } // e.g. 28        

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
            var multiplier = (InitialTrailPercentage - FinalTrailPercentage) / (ProfitTargetPercentage - InitiateAtGainPercentage.Value);
            var value = InitialTrailPercentage - (gain - InitiateAtGainPercentage.Value) * multiplier;

            return value;
        }
    }
}