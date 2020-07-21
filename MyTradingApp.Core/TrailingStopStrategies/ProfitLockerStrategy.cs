namespace MyTradingApp.TrailingStopStrategies
{
    public abstract class ProfitLockerStrategy : TrailingStopStrategy
    {
        protected ProfitLockerStrategy()
        {
        }

        public double ProfitTarget { get; set; }

        public abstract double ResumingTrailingAtProfitTarget { get; }
    }
}
