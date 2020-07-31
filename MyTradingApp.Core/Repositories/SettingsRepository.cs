using Microsoft.EntityFrameworkCore;
using MyTradingApp.Domain;
using MyTradingApp.Persistence;
using MyTradingApp.Repositories;
using System.Collections.Generic;
using System.Linq;
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

        public async Task SaveAsync(IEnumerable<Setting> settings)
        {
            var toAdd = new List<Setting>();
            var getSettingsTask = GetAllAsync().ConfigureAwait(false);
            var existingSettings = (await getSettingsTask).ToList();
            foreach (var setting in settings)
            {
                var existingSetting = existingSettings.SingleOrDefault(x => x.Key == setting.Key);
                if (existingSetting == null)
                {
                    toAdd.Add(setting);
                }
                else
                {
                    existingSetting = Context.Settings.Find(existingSetting.Key);
                    existingSetting.Value = setting.Value;
                    Context.Settings.Attach(existingSetting);
                }                
            }

            Context.Settings.AddRange(toAdd);
            await Context.SaveChangesAsync().ConfigureAwait(false);
        }
    }
}