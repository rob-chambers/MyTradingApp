namespace MyTradingApp.ProfitLocker.StopTypes
{
    public abstract class StopType
    {
        public abstract string OrderType { get; }

        public abstract StopValue GetStopValue(StopLossRule rule, double percentage);
    }
}
