using Microsoft.EntityFrameworkCore;
using MyTradingApp.Domain;
using System.Threading;
using System.Threading.Tasks;

namespace MyTradingApp.Persistence
{
    public interface IApplicationContext
    {
        DbSet<StopLoss> Stops { get; set; }
        DbSet<Trade> Trades { get; set; }
        DbSet<Setting> Settings { get; set; }

        Task<int> SaveChangesAsync(CancellationToken token = default);
    }
}