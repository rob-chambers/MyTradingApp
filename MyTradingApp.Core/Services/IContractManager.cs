using IBApi;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MyTradingApp.Services
{
    public interface IContractManager
    {
        Task<IList<ContractDetails>> RequestDetailsAsync(Contract contract);
    }
}