using MyTradingApp.Models;

namespace MyTradingApp.EventMessages
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
