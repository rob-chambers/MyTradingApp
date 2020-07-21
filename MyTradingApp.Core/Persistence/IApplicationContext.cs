using Microsoft.EntityFrameworkCore;
using MyTradingApp.Domain;

namespace MyTradingApp.Persistence
{
    public interface IApplicationContext
    {
        DbSet<StopLoss> Stops { get; set; }
        DbSet<Trade> Trades { get; set; }
        DbSet<Setting> Settings { get; set; }

        int SaveChanges();
    }
}