using AutoFinance.Broker.InteractiveBrokers.Controllers;
using IBApi;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MyTradingApp.Core.Services
{
    public class ContractManager : IContractManager
    {
        private readonly ITwsObjectFactory _twsObjectFactory;

        public ContractManager(ITwsObjectFactory twsObjectFactory)
        {
            _twsObjectFactory = twsObjectFactory;
        }

        public Task<List<ContractDetails>> RequestDetailsAsync(Contract contract)
        {
            return _twsObjectFactory.TwsController.GetContractAsync(contract);
        }
    }
}