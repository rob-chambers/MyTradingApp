using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using MyTradingApp.Core.Persistence;
using MyTradingApp.Domain;
using System.IO;

namespace MyTradingApp.Desktop
{
    /// <summary>
    /// A factory class for instantiating a design-time EF Core Application Context.
    /// </summary>
    internal class ApplicationContextFactory : IDesignTimeDbContextFactory<ApplicationContext>
    {
        public ApplicationContext CreateDbContext(string[] args)
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

            var configuration = builder.Build();
            var optionsBuilder = new DbContextOptionsBuilder<ApplicationContext>();
            var connectionString = ConfigurationExtensions.GetConnectionString(configuration, Settings.DefaultConnection);
            optionsBuilder.UseSqlServer(connectionString);

            return new ApplicationContext(optionsBuilder.Options);
        }
    }
}
