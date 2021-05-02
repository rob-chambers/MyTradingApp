namespace MyTradingApp.ProfitLocker.StopTypes
{
    public class FloatingStop : StopType
    {
        public override string OrderType => "STP";

        public override StopValue GetStopValue(StopLossRule rule, double percentage)
        {
            return null;
        }
    }
}