using MyTradingApp.Domain;
using System.Data.Entity;

namespace MyTradingApp.Persistence
{
    public interface IApplicationContext
    {
        DbSet<StopLoss> Stops { get; set; }
        DbSet<Trade> Trades { get; set; }

        int SaveChanges();
    }
}