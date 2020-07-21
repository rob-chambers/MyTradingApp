using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using MyTradingApp.Core.Persistence;
using MyTradingApp.Domain;

namespace MyTradingApp.Persistence
{
    public class ApplicationContext : DbContext, IApplicationContext
    {
        private readonly IConfigurationProvider _configurationProvider;

        public ApplicationContext(DbContextOptions options) : base(options)
        {
        }

        public DbSet<Trade> Trades { get; set; }
        public DbSet<StopLoss> Stops { get; set; }
        public DbSet<Setting> Settings { get; set; }

        //protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        //{
        //    _configurationProvider.TryGet("ConnectionString", out var connectionString);
        //    optionsBuilder.UseSqlServer(connectionString);
        //}

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.RemovePluralizingTableNameConvention();
            base.OnModelCreating(modelBuilder);
        }
    }
}