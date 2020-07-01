namespace MyTradingApp.TrailingStopStrategies
{
    internal abstract class TrailingStopStrategy
    {
        protected TrailingStopStrategy()
        {
        }

        public double Risk { get; set; }

        public abstract double CalculateTrailingStopPercentage(double gainPercentage);
    }
}
