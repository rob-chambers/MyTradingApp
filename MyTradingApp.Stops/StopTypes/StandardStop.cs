using MyTradingApp.Domain;

namespace MyTradingApp.Stops.StopTypes
{
    public class StandardStop : Stop
    {
        private double _latestStop;

        public double InitialTrailPercentage { get; set; }

        public override StopType Type => StopType.Standard;

        public override void CalculatePrice(Position position, double gainPercentage, double high, double low)
        {
            if (_latestStop != 0)
            {
                Price = _latestStop;
                return;
            }

            if (position.Direction == Direction.Buy)
            {
                Price = high - high * InitialTrailPercentage / 100D;
            }
            else
            {
                Price = low + low * InitialTrailPercentage / 100D;
            }

            _latestStop = Price;
        }

        public override void Reset()
        {
            _latestStop = 0;
        }
    }
}