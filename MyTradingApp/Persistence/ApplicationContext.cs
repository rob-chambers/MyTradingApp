using MyTradingApp.Domain;
using System.Data.Entity;
using System.Data.Entity.ModelConfiguration.Conventions;

namespace MyTradingApp.Persistence
{
    public class ApplicationContext : DbContext, IApplicationContext
    {
        public ApplicationContext() : base("MyTradingAppContext")
        {
        }

        public DbSet<Trade> Trades { get; set; }
        public DbSet<StopLoss> Stops { get; set; }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            modelBuilder.Conventions.Remove<PluralizingTableNameConvention>();
        }
    }
}