using MyTradingApp.Domain;

namespace MyTradingApp.EventMessages
{
    public class FundamentalDataMessage : SymbolMessage
    {
        public FundamentalDataMessage(string symbol, FundamentalData data) 
            : base(symbol)
        {
            Data = data;
        }

        public FundamentalData Data { get; }
    }
}