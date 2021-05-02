namespace MyTradingApp.ProfitLocker
{
    public class StopLossRule
    {
        public StopTypeValue StopType { get; set; }

        public double? Percentage { get; set; }

        public double LowerProfitPercentage { get; set; }

        public double UpperProfitPercentage { get; set; }
    }
}
