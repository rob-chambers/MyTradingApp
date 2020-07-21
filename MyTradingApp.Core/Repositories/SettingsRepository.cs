using Microsoft.EntityFrameworkCore;
using MyTradingApp.Domain;
using MyTradingApp.Persistence;
using MyTradingApp.Repositories;
using System.Collections.Generic;
using System.Linq;

namespace MyTradingApp.Core.Repositories
{
    public class SettingsRepository : EfRepository, ISettingsRepository
    {
        public SettingsRepository(IApplicationContext context) : base(context)
        {
        }

        public IEnumerable<Setting> GetAll()
        {
            return Context.Settings
                .AsNoTracking()
                .ToList();
        }

        public void Save(IEnumerable<Setting> settings)
        {
            //var item = Context.Settings.FirstOrDefault();
            //if (item != null)
            //{
            //    Context.Settings.Remove(item);
            //}

            //Context.Settings.Add(setting);
            //Context.SaveChanges();

            var toAdd = new List<Setting>();
            var existingSettings = GetAll().ToList();
            foreach (var setting in settings)
            {
                var existingSetting = existingSettings.SingleOrDefault(x => x.Key == setting.Key);
                if (existingSetting == null)
                {
                    toAdd.Add(setting);                    
                }
                else
                {
                    var item = Context.Settings.Find(existingSetting.Key);
                    item.Value = setting.Value;
                }
            }

            Context.Settings.AddRange(toAdd);
            Context.SaveChanges();
        }
    }
}
