using Microsoft.EntityFrameworkCore;
using MyTradingApp.Core.Persistence;
using MyTradingApp.Domain;
using Serilog;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MyTradingApp.Core.Repositories
{
    public class TradeRepository : EfRepository, ITradeRepository
    {
        public TradeRepository(IApplicationContext context) 
            : base(context)
        {
        }

        public async Task AddExitAsync(Exit exit)
        {
            Log.Information("Adding exit");
            Context.Trades.Attach(exit.Trade);
            Context.Exits.Add(exit);
            await Context.SaveChangesAsync().ConfigureAwait(false);
        }

        public async Task AddTradeAsync(Trade trade)
        {
            Context.Trades.Add(trade);
            await Context.SaveChangesAsync().ConfigureAwait(false);
        }

        public async Task<IEnumerable<Trade>> GetAllOpenAsync()
        {
            var trades = Context.Trades
                .AsNoTracking()
                .Where(t => !t.ExitTimeStamp.HasValue);
            return await trades.ToListAsync();
        }

        public async Task UpdateAsync(Trade trade)
        {
            Context.Trades.Attach(trade);
            await Context.SaveChangesAsync().ConfigureAwait(false);
        }
    }
}