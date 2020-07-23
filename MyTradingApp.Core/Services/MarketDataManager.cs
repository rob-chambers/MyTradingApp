using AutoFinance.Broker.InteractiveBrokers.Controllers;
using AutoFinance.Broker.InteractiveBrokers.EventArgs;
using GalaSoft.MvvmLight.Messaging;
using IBApi;
using MyTradingApp.EventMessages;
using MyTradingApp.Messages;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MyTradingApp.Services
{
    public class MarketDataManager : IMarketDataManager
    {
        public const int TICK_ID_BASE = 10000000;

        private readonly Dictionary<int, Tuple<Contract, bool>> _activeRequests = new Dictionary<int, Tuple<Contract, bool>>();
        private readonly IBClient _iBClient;
        private readonly ITwsObjectFactory _twsObjectFactory;
        private readonly Dictionary<string, Domain.Bar> _prices = new Dictionary<string, Domain.Bar>();
        private int _currentTicker = 1;
        private TickPriceEventArgs _tickPriceEventArgs;

        public MarketDataManager(IBClient iBClient, ITwsObjectFactory twsObjectFactory)
        {
            iBClient.TickPrice += OnTickPrice;
            _iBClient = iBClient;
            _twsObjectFactory = twsObjectFactory;
        }

        protected void OnTickPrice(TickPriceMessage msg)
        {
            if (msg.Price == -1) return;  // Market is probably closed

            if (!_activeRequests.ContainsKey(msg.RequestId))
            {
                Log.Warning("Get price for an unexpected request id of {0}", msg.RequestId);
                return;
            }

            var tuple = _activeRequests[msg.RequestId];
            var symbol = tuple.Item1.Symbol;
            if (!tuple.Item2)
            {
                switch (msg.Field)
                {
                    case TickType.LAST:
                    // Fall-through

                    // Don't include Ask price any more
                    //case TickType.ASK:
                        Messenger.Default.Send(new TickPrice(symbol, msg.Field, msg.Price));
                        break;
                }
            }
            else
            {
                HandleTickPriceForOhlcRequest(msg, symbol);
            }
        }

        private void HandleTickPriceForOhlcRequest(TickPriceMessage msg, string symbol)
        {
            var type = string.Empty;

            if (!_prices.ContainsKey(symbol))
            {
                _prices.Add(symbol, new Domain.Bar
                {
                    Date = DateTime.UtcNow
                });
            }
            var bar = _prices[symbol];

            switch (msg.Field)
            {
                case TickType.OPEN:
                    type = "O";
                    bar.Open = msg.Price;
                    break;
                case TickType.HIGH:
                    type = "H";
                    bar.High = msg.Price;
                    break;
                case TickType.LOW:
                    type = "L";
                    bar.Low = msg.Price;
                    break;
                case TickType.CLOSE:
                    type = "C";
                    bar.Close = msg.Price;
                    break;
            }

            if (type != string.Empty && bar.Open != 0 && bar.Close != 0 && bar.High != 0 && bar.Low != 0)
            {
                //Log.Debug(msg.DumpToString($"New {type} price for {symbol}"));
                Messenger.Default.Send(new BarPriceMessage(symbol, bar));
            }
        }

        public void RequestStreamingPrice(Contract contract, bool ohlc = false)
        {
            Log.Debug("RequestStreamingPrice for contract {0}", contract.Symbol);
            var nextRequestId = TICK_ID_BASE + _currentTicker++;
            RequestMarketData(contract, nextRequestId);
            _activeRequests.Add(nextRequestId, new Tuple<Contract, bool>(contract, ohlc));
        }

        protected virtual void RequestMarketData(Contract contract, int nextRequestId)
        {
            _iBClient.ClientSocket.reqMktData(nextRequestId, contract, string.Empty, false, false, new List<TagValue>());
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
                .Where(x => x.Value.Item1.Symbol == symbol)
                .Select(x => new { x.Key, x.Value })
                .FirstOrDefault();

            if (request != null)
            {
                _iBClient.ClientSocket.cancelMktData(request.Key);
            }
        }

        public async Task<double> RequestLatestPriceAsync(Contract contract)
        {
            _twsObjectFactory.TwsCallbackHandler.TickPriceEvent += OnTickPriceEvent;
            var marketDataResult = await _twsObjectFactory.TwsControllerBase.RequestMarketDataAsync(contract, string.Empty, true, false, null);
            _twsObjectFactory.TwsCallbackHandler.TickPriceEvent -= OnTickPriceEvent;
            if (_tickPriceEventArgs != null)
            {
                return _tickPriceEventArgs.Price;
            }

            Log.Warning("Couldn't get price for {0}", contract.Symbol);
            return 0;
        }

        private void OnTickPriceEvent(object sender, TickPriceEventArgs args)
        {
            _tickPriceEventArgs = args;
        }
    }
}