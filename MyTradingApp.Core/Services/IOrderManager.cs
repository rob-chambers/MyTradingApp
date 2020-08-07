using IBApi;
using System.Threading.Tasks;

namespace MyTradingApp.Core.Services
{
    public interface IOrderManager
    {
        Task PlaceNewOrderAsync(Contract contract, Order order);
    }
}