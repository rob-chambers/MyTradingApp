using MyTradingApp.Domain;
using System.Collections.Generic;

namespace MyTradingApp.Core.Repositories
{
    public interface ISettingsRepository
    {
        IEnumerable<Setting> GetAll();
        void Save(IEnumerable<Setting> settings);
    }
}
