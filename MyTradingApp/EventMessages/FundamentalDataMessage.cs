namespace MyTradingApp.Models
{
    public class FundamentalDataMessage
    {
        public FundamentalDataMessage(FundamentalData data)
        {
            Data = data;
        }

        public FundamentalData Data { get; }
    }
}
