using GalaSoft.MvvmLight.Messaging;
using IBApi;
using MyTradingApp.EventMessages;

namespace MyTradingApp.Services
{
    internal class ExchangeRateService : IExchangeRateService
    {
        private const string ExchangeRatePair = "AUD";
        private readonly IMarketDataManager _marketDataManager;

        public ExchangeRateService(IMarketDataManager marketDataManager)
        {
            _marketDataManager = marketDataManager;
            Messenger.Default.Register<TickPrice>(this, HandleTickPriceMessage);
        }

        public void RequestExchangeRate()
        {
            var contract = GetExchangeRateContract();
            _marketDataManager.RequestLatestPrice(contract);
        }

        private Contract GetExchangeRateContract()
        {
            var contract = new Contract
            {
                Symbol = ExchangeRatePair,
                SecType = "CASH",
                Exchange = "IDEALPRO",
                Currency = "USD",
                LastTradeDateOrContractMonth = string.Empty,
                Multiplier = string.Empty,
                LocalSymbol = string.Empty
            };

            return contract;
        }

        private void HandleTickPriceMessage(TickPrice tickPrice)
        {
            // Was it a request for the exchange rate?
            if (tickPrice.Symbol == ExchangeRatePair)
            {
                Messenger.Default.Send(new ExchangeRateMessage(tickPrice.Price));                
            }
        }
    }
}
