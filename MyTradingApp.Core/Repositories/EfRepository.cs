using MyTradingApp.Persistence;

namespace MyTradingApp.Repositories
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
