using MyTradingApp.Domain;
using MyTradingApp.Persistence;
using System.Threading.Tasks;

namespace MyTradingApp.Repositories
{
    public class TradeRepository : EfRepository, ITradeRepository
    {
        public TradeRepository(IApplicationContext context) : base(context)
        {
        }

        public async Task AddTradeAsync(Trade trade)
        {
            Context.Trades.Add(trade);
            await Context.SaveChangesAsync().ConfigureAwait(false);
        }
    }
}