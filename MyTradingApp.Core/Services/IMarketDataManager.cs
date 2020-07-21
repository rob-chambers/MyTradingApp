﻿using IBApi;

namespace MyTradingApp.Services
{
    public interface IMarketDataManager
    {
        void RequestStreamingPrice(Contract contract, bool ohlc = false);

        void StopActivePriceStreaming();

        void RequestLatestPrice(Contract contract);

        void StopPriceStreaming(string symbol);
    }
}