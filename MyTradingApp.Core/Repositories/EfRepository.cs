using MyTradingApp.Core.Persistence;

namespace MyTradingApp.Core.Repositories
{
    public abstract class EfRepository
    {
        protected EfRepository(IApplicationContext context)
        {
            Context = context;
        }

        protected IApplicationContext Context { get; }
    }
}
