using AutoFinance.Broker.InteractiveBrokers.Controllers;
using IBApi;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MyTradingApp.Services
{
    public class ContractManager : IContractManager
    {
        private readonly ITwsObjectFactory _twsObjectFactory;

        public ContractManager(ITwsObjectFactory twsObjectFactory)
        {
            _twsObjectFactory = twsObjectFactory;
        }

        public async Task<IList<ContractDetails>> RequestDetailsAsync(Contract contract)
        {
            return await _twsObjectFactory.TwsController.GetContractAsync(contract);
        }
    }
}