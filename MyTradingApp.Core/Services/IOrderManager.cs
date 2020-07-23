using IBApi;
using System.Threading.Tasks;

namespace MyTradingApp.Services
{
    public interface IOrderManager
    {
        Task PlaceNewOrderAsync(Contract contract, Order order);
    }
}