using MyTradingApp.Models;

namespace MyTradingApp.Repositories
{
    public interface ITradeRepository
    {
        void AddTrade(Trade trade);
    }
}