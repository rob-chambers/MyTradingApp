using IBApi;
using MyTradingApp.Messages;
using MyTradingApp.Models;
using System;

namespace MyTradingApp.Services
{
    public interface IContractManager
    {
        event EventHandler<FundamentalDataEventArgs> FundamentalData;

        void RequestFundamentals(Contract contract, string reportType);
    }
}