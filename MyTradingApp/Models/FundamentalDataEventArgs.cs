using System;

namespace MyTradingApp.Models
{
    public class FundamentalDataEventArgs : EventArgs
    {
        public FundamentalDataEventArgs(FundamentalData data)
        {
            Data = data;
        }

        public FundamentalData Data { get; }
    }
}
