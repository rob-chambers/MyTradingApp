using MyTradingApp.Domain;
using MyTradingApp.Persistence;

namespace MyTradingApp.Repositories
{
    public class TradeRepository : EfRepository, ITradeRepository
    {
        public TradeRepository(IApplicationContext context) : base(context)
        {
        }

        public void AddTrade(Trade trade)
        {
            Context.Trades.Add(trade);
            Context.SaveChanges();
        }
    }
}
