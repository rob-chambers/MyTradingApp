using Microsoft.EntityFrameworkCore;
using MyTradingApp.Core.Persistence;
using MyTradingApp.Domain;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MyTradingApp.Core.Repositories
{
    public class SettingsRepository : EfRepository, ISettingsRepository
    {
        public SettingsRepository(IApplicationContext context) : base(context)
        {
        }

        public async Task<IEnumerable<Setting>> GetAllAsync()
        {
            return await Context.Settings                
                .AsNoTracking()
                .ToListAsync();
        }

        public void Add(Setting setting)
        {
            Context.Settings.Add(setting);
        }

        public void Update(Setting setting)
        {
            Context.Settings.Attach(setting);
        }

        public Task SaveAsync()
        {
            return Context.SaveChangesAsync();            
        }
    }
}