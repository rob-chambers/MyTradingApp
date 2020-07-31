using MyTradingApp.Domain;
using System.Threading.Tasks;

namespace MyTradingApp.Repositories
{
    public interface ITradeRepository
    {
        Task AddTradeAsync(Trade trade);
    }
}