namespace MyTradingApp.Models
{
    public class FundamentalData
    {
        public string CompanyName { get; set; }

        public static FundamentalData Parse(string data)
        {
            return new FundamentalData
            {
                CompanyName = "Microsoft"
            };
        }
    }
}
