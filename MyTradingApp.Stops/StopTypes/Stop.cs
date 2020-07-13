namespace MyTradingApp.Stops.StopTypes
{
    public abstract class Stop
    {
        public double? InitiateAtGainPercentage { get; set; }

        public double Price { get; set; }

        public abstract StopType Type { get; }

        public abstract void CalculatePrice(Position position, double gainPercentage, double high, double low);

        public virtual void Reset()
        {
        }
    }
}