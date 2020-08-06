using AutoFinance.Broker.InteractiveBrokers.Controllers;
using AutoFinance.Broker.InteractiveBrokers.EventArgs;
using GalaSoft.MvvmLight.Messaging;
using IBApi;
using MyTradingApp.EventMessages;
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
        private readonly ITwsObjectFactory _twsObjectFactory;
        private readonly Dictionary<string, Domain.Bar> _prices = new Dictionary<string, Domain.Bar>();
        private readonly Dictionary<string, double> _oneOffPrices = new Dictionary<string, double>();
        private TickPriceEventArgs _tickPriceEventArgs;
        private bool _tickHandlerAttached;

        public MarketDataManager(ITwsObjectFactory twsObjectFactory)
        {
            _twsObjectFactory = twsObjectFactory;
        }

        public async Task<int> RequestStreamingPriceAsync(Contract contract)
        {
            Log.Debug("Requesting streaming price for {0}", contract.Symbol);

            if (!_tickHandlerAttached)
            { 
                // TODO: When do we remove event handler?
                _twsObjectFactory.TwsCallbackHandler.TickPriceEvent += OnTickPriceEvent;
            }

            var tick = await _twsObjectFactory.TwsControllerBase.RequestMarketDataAsync(contract, "233", false, false, null);
            Log.Debug("Ticker id for {0}: {1}", contract.Symbol, tick.TickerId);

            return tick.TickerId;
        }

        public void StopActivePriceStreaming(IEnumerable<int> tickerIds)
        {
            Log.Debug("Stopping active price streaming");
            foreach (var request in tickerIds)
            {
                _twsObjectFactory.TwsController.CancelMarketData(request);
            }
        }

        public void StopPriceStreaming(string symbol)
        {
            Log.Debug("Stopping streaming for {0}", symbol);

            var request = _activeRequests
                .Where(x => x.Value.Item2 && x.Value.Item1.Symbol == symbol)
                .Select(x => new { x.Key, x.Value })
                .FirstOrDefault();

            if (request != null)
            {
                _twsObjectFactory.TwsController.CancelMarketData(request.Key);
                _activeRequests.Remove(request.Key);
            }
        }

        public async Task<double> RequestLatestPriceAsync(Contract contract)
        {
            Log.Debug("Requesting Latest Price");
            if (!_tickHandlerAttached)
            {
                _twsObjectFactory.TwsCallbackHandler.TickPriceEvent += OnTickPriceEvent;
                _tickHandlerAttached = true;
            }
            
            var result = await _twsObjectFactory.TwsControllerBase.RequestMarketDataAsync(contract, string.Empty, true, false, null);
            Log.Debug("Ticker assigned to {0} = {1}", contract.Symbol, result.TickerId);
            _activeRequests.Add(result.TickerId, new Tuple<Contract, bool>(contract, false));

            if (_oneOffPrices.ContainsKey(contract.Symbol))
            {
                return _oneOffPrices[contract.Symbol];
            }

            if (_tickPriceEventArgs != null && _tickPriceEventArgs.TickerId == result.TickerId)
            {
                return _tickPriceEventArgs.Price;
            }

            return 0;

            // TODO: Remove handler on disconnection?
            //_twsObjectFactory.TwsCallbackHandler.TickPriceEvent -= OnTickPriceEvent;
            //if (_tickPriceEventArgs != null)
            //{
            //    return _tickPriceEventArgs.Price;
            //}

            //Log.Warning("Couldn't get price for {0}", contract.Symbol);
            //return 0;
        }

        private void OnTickPriceEvent(object sender, TickPriceEventArgs args)
        {            
            _tickPriceEventArgs = args;          
            if (!_activeRequests.ContainsKey(args.TickerId))
            {
                // Request latest price (not streaming)
                return;
            }

            var request = _activeRequests[args.TickerId];
            var symbol = request.Item1.Symbol;

            if (request.Item2)
            {
                // Streaming request
                if (!_prices.ContainsKey(symbol))
                {
                    _prices.Add(symbol, new Domain.Bar
                    {
                        Date = DateTime.UtcNow
                    });
                }
                var bar = _prices[symbol];

                switch (args.Field)
                {
                    case TickType.OPEN:
                        bar.Open = args.Price;
                        break;
                    case TickType.HIGH:
                        bar.High = args.Price;
                        break;
                    case TickType.LOW:
                        bar.Low = args.Price;
                        break;
                    case TickType.CLOSE:
                        bar.Close = args.Price;
                        break;

                    case TickType.LAST:
                        // Send new messages out whenever a trade was made - i.e. type = Last
                        bar.Close = args.Price;
                        Messenger.Default.Send(new BarPriceMessage(symbol, bar));

                        // Flag for creation of a new bar on next tick
                        _prices.Remove(symbol);
                        break;
                }
            }
            else
            {
                if (args.Field == TickType.LAST)
                {
                    if (_oneOffPrices.ContainsKey(symbol))
                    {
                        _oneOffPrices.Remove(symbol);
                    }

                    _oneOffPrices.Add(symbol, args.Price);
                }                       
            }
        }
    }
}