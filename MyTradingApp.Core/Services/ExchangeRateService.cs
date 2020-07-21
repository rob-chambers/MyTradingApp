using AutoFinance.Broker.InteractiveBrokers.Constants;
using AutoFinance.Broker.InteractiveBrokers.Controllers;
using AutoFinance.Broker.InteractiveBrokers.EventArgs;
using IBApi;
using System.Threading.Tasks;

namespace MyTradingApp.Services
{
    public class ExchangeRateService : IExchangeRateService
    {
        private const double DefaultExchangeRate = 0.7;
        private readonly ITwsObjectFactory _twsObjectFactory;
        private TickPriceEventArgs _tickPriceEventArgs;

        public ExchangeRateService(ITwsObjectFactory twsObjectFactory)
        {
            _twsObjectFactory = twsObjectFactory;
        }

        public async Task<double> GetExchangeRateAsync()
        {
            var twsController = _twsObjectFactory.TwsControllerBase;

            // Initialize the contract
            var contract = new Contract
            {
                SecType = TwsContractSecType.Cash,
                Symbol = "AUD",
                Exchange = TwsExchange.Idealpro,
                Currency = TwsCurrency.Usd
            };

            // Call the API
            _twsObjectFactory.TwsCallbackHandler.TickPriceEvent += OnTickPriceEvent;
            var marketDataResult = await _twsObjectFactory.TwsControllerBase.RequestMarketDataAsync(contract, string.Empty, true, false, null);
            _twsObjectFactory.TwsCallbackHandler.TickPriceEvent -= OnTickPriceEvent;

            return _tickPriceEventArgs == null
                ? DefaultExchangeRate
                : _tickPriceEventArgs.Price;
        }

        private void OnTickPriceEvent(object sender, TickPriceEventArgs args)
        {
            _tickPriceEventArgs = args;
        }
    }
}