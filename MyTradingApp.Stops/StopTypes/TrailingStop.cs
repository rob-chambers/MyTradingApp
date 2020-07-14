namespace MyTradingApp.Stops.StopTypes
{
    public class TrailingStop : Stop
    {
        public override StopType Type => StopType.Trailing;

        public double Percentage { get; set; }        

        public override void CalculatePrice(Position position, double gainPercentage, double high, double low)
        {
            if (position.Direction == TradeDirection.Long)
            {
                Price = high - high * Percentage / 100D;
            }
            else
            {
                Price = low + low * Percentage / 100D;
            }
        }
    }
}