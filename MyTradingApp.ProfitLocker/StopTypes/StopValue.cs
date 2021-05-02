namespace MyTradingApp.ProfitLocker.StopTypes
{
    public class StopValue
    {
        /// <summary>
        /// Maybe we can remove this?
        /// </summary>
        public double Price { get; set; }

        public double TrailingPercent { get; set; }
    }
}
