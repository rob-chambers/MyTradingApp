namespace MyTradingApp.EventMessages
{
    public class AccountSummaryCompletedMessage
    {
        public string AccountId { get; set; }
        public double NetLiquidation { get; set; }
        public double BuyingPower { get; set; }        
    }
}