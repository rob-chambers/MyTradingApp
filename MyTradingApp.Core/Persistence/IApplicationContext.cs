using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using MyTradingApp.Domain;
using System.Threading;
using System.Threading.Tasks;

namespace MyTradingApp.Core.Persistence
{
    public interface IApplicationContext
    {
        DatabaseFacade Database
        {
            get;
        }

        DbSet<Trade> Trades
        {
            get; set;
        }

        DbSet<Exit> Exits
        {
            get; set;
        }

        DbSet<StopLoss> Stops
        {
            get; set;
        }

        DbSet<Setting> Settings
        {
            get; set;
        }

        Task<int> SaveChangesAsync(CancellationToken token = default);
    }
}