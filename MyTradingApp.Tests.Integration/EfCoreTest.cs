using Microsoft.EntityFrameworkCore;
using MyTradingApp.Core.Persistence;

namespace MyTradingApp.Tests.Integration
{
    public abstract class EfCoreTest
    {
        public EfCoreTest(DbContextOptions<ApplicationContext> contextOptions)
        {
            ContextOptions = contextOptions;
            Context = new ApplicationContext(ContextOptions);
            Seed();
        }

        public ApplicationContext Context { get; }

        protected DbContextOptions<ApplicationContext> ContextOptions { get; }
               
        protected abstract void Seed();
    }
}
