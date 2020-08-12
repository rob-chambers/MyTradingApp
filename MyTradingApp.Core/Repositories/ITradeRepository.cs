using MyTradingApp.Domain;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MyTradingApp.Core.Repositories
{
    public interface ITradeRepository
    {
        Task AddTradeAsync(Trade trade);

        Task<IEnumerable<Trade>> GetAllOpenAsync();

        Task AddExitAsync(Exit exit);
        
        Task UpdateAsync(Trade trade);
    }
}