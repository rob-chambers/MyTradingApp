using MyTradingApp.Domain;
using System.Threading.Tasks;

namespace MyTradingApp.Core.Repositories
{
    public interface ITradeRepository
    {
        Task AddTradeAsync(Trade trade);
    }
}