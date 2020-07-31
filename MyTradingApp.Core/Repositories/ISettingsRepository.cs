using MyTradingApp.Domain;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MyTradingApp.Core.Repositories
{
    public interface ISettingsRepository
    {
        Task<IEnumerable<Setting>> GetAllAsync();

        Task SaveAsync(IEnumerable<Setting> settings);
    }
}