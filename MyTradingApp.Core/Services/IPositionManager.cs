using AutoFinance.Broker.InteractiveBrokers.EventArgs;
using IBApi;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MyTradingApp.Core.Services
{
    public interface IPositionManager
    {
        Task<List<OpenOrderEventArgs>> RequestOpenOrdersAsync();

        Task UpdateStopOrderAsync(Contract contract, Order order);
    }
}