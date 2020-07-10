using GalaSoft.MvvmLight.Messaging;
using IBApi;
using MyTradingApp.EventMessages;
using MyTradingApp.Messages;
using Serilog;
using System.Collections.Generic;
using System.Linq;

namespace MyTradingApp.Services
{
    internal class MarketDataManager : IMarketDataManager
    {
        public const int TICK_ID_BASE = 10000000;
        public const int TICK_ID_BASE_ONE_OFF = TICK_ID_BASE + 10000;

        private readonly Dictionary<int, Contract> _activeRequests = new Dictionary<int, Contract>();
        private readonly IBClient _iBClient;
        private int _currentTicker = 1;
        private int _latestPriceTicker = 1;

        public MarketDataManager(IBClient iBClient)
        {
            iBClient.TickPrice += OnTickPrice;
            _iBClient = iBClient;
        }

        private void OnTickPrice(TickPriceMessage msg)
        {
            if (msg.Price == -1) return;  // Market is probably closed

            if (!_activeRequests.ContainsKey(msg.RequestId))
            {
                Log.Warning("Get price for an unexpected request id of {0}", msg.RequestId);
                return;
            }

            var symbol = _activeRequests[msg.RequestId].Symbol;
            switch (msg.Field)
            {
                case TickType.LAST:
                    // Fall-through
                case TickType.ASK:
                    Messenger.Default.Send(new TickPrice(symbol, msg.Field, msg.Price));
                    break;
            }
        }

        public void RequestStreamingPrice(Contract contract)
        {
            var nextRequestId = TICK_ID_BASE + _currentTicker++;
            _iBClient.ClientSocket.reqMktData(nextRequestId, contract, string.Empty, false, false, new List<TagValue>());
            _activeRequests.Add(nextRequestId, contract);
        }

        public void StopActivePriceStreaming()
        {
            for (var i = 1; i < _currentTicker; i++)
            {
                _iBClient.ClientSocket.cancelMktData(i + TICK_ID_BASE);
            }
        }

        public void StopPriceStreaming(string symbol)
        {
            var request = _activeRequests
                .Where(x => x.Value.Symbol == symbol)
                .Select(x => new { x.Key, x.Value })
                .FirstOrDefault();

            if (request != null)
            {
                _iBClient.ClientSocket.cancelMktData(request.Key);
            }
        }

        public void RequestLatestPrice(Contract contract)
        {
            var nextRequestId = TICK_ID_BASE_ONE_OFF + _latestPriceTicker++;
            _iBClient.ClientSocket.reqMktData(nextRequestId, contract, string.Empty, true, false, new List<TagValue>());
            _activeRequests.Add(nextRequestId, contract);
        }
    }
}