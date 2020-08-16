using IBApi;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MyTradingApp.Core.Services
{
    public interface IContractManager
    {
        Task<List<ContractDetails>> RequestDetailsAsync(Contract contract);
    }
}