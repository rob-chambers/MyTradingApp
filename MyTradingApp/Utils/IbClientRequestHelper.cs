using MyTradingApp.Domain;

namespace MyTradingApp.Utils
{
    internal static class IbClientRequestHelper
    {
        public static string MapExchange(Exchange exchange)
        {
            switch (exchange)
            {
                // On https://interactivebrokers.github.io/tws-api/basic_contracts.html, it mentions that stocks on the Nasdaq should be routed through ISLAND
                case Exchange.Nasdaq:
                    return BrokerConstants.Routers.Island;

                case Exchange.London:
                    return "LSE";
            }

            return exchange.ToString();
        }
    }
}
