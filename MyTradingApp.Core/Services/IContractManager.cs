using IBApi;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MyTradingApp.Services
{
    public interface IContractManager
    {
        void RequestFundamentals(Contract contract, string reportType);

        Task<IList<ContractDetails>> RequestDetailsAsync(Contract contract);
    }
}