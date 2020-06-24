﻿using GalaSoft.MvvmLight.Messaging;
using IBApi;
using MyTradingApp.EventMessages;
using MyTradingApp.Messages;
using System.Collections.Generic;
using System.Diagnostics;

namespace MyTradingApp.Services
{
    internal class MarketDataManager : DataManager, IMarketDataManager
    {
        public const int TICK_ID_BASE = 10000000;
        public const int TICK_ID_BASE_ONE_OFF = TICK_ID_BASE + 1000;

        private readonly Dictionary<int, Contract> _activeRequests = new Dictionary<int, Contract>();
        private int _currentTicker = 1;
        private int _latestPriceTicker = 1;

        public MarketDataManager(IBClient iBClient) : base(iBClient)
        {
            iBClient.TickPrice += OnTickPrice;
        }

        private void OnTickPrice(TickPriceMessage msg)
        {
            if (msg.Price == -1) return;  // Market is probably closed

            string symbol;
            switch (msg.Field)
            {
                case TickType.LAST:
                    symbol = _activeRequests[msg.RequestId].Symbol;
                    Messenger.Default.Send(new TickPrice(symbol, msg.Price));
                    break;

                case TickType.ASK:
                    symbol = _activeRequests[msg.RequestId].Symbol;
                    if (msg.RequestId >= TICK_ID_BASE_ONE_OFF)
                    {
                        // This is a one-off request - cancel further requests
                        Debug.WriteLine("Received one-off request {0}", msg.RequestId);
                        //ibClient.ClientSocket.cancelMktData(msg.RequestId);
                    }

                    Messenger.Default.Send(new TickPrice(symbol, msg.Price));
                    break;
            }
        }

        public void RequestStreamingPrice(Contract contract)
        {
            var nextRequestId = TICK_ID_BASE + _currentTicker++;
            ibClient.ClientSocket.reqMktData(nextRequestId, contract, string.Empty, false, false, new List<TagValue>());
            _activeRequests.Add(nextRequestId, contract);
        }

        public void StopActivePriceStreaming()
        {
            for (var i = 1; i < _currentTicker; i++)
            {
                ibClient.ClientSocket.cancelMktData(i + TICK_ID_BASE);
            }
        }

        public override void NotifyError(int requestId)
        {
            throw new System.NotImplementedException();
        }

        public override void Clear()
        {
            currentTicker = 1;
        }

        public void RequestLatestPrice(Contract contract)
        {
            var nextRequestId = TICK_ID_BASE_ONE_OFF + _latestPriceTicker++;
            ibClient.ClientSocket.reqMktData(nextRequestId, contract, string.Empty, true, false, new List<TagValue>());
            _activeRequests.Add(nextRequestId, contract);
        }
    }
}