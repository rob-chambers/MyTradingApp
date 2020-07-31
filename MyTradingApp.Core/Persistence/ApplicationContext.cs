using Microsoft.EntityFrameworkCore;
using MyTradingApp.Core.Persistence;
using MyTradingApp.Domain;

namespace MyTradingApp.Persistence
{
    public class ApplicationContext : DbContext, IApplicationContext
    {
        public ApplicationContext(DbContextOptions options) : base(options)
        {
        }

        public DbSet<Trade> Trades { get; set; }
        public DbSet<StopLoss> Stops { get; set; }
        public DbSet<Setting> Settings { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.RemovePluralizingTableNameConvention();
            base.OnModelCreating(modelBuilder);
        }
    }
}