using MyTradingApp.Domain;

namespace MyTradingApp.Repositories
{
    public interface ITradeRepository
    {
        void AddTrade(Trade trade);
    }
}