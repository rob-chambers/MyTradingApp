using AutoFinance.Broker.InteractiveBrokers.Controllers;
using GalaSoft.MvvmLight.Messaging;
using IBApi;
using MyTradingApp.Domain;
using MyTradingApp.EventMessages;
using MyTradingApp.Messages;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MyTradingApp.Services
{
    public class ContractManager : IContractManager
    {
        public const int CONTRACT_ID_BASE = 60000000;        
        public const int FUNDAMENTALS_ID = CONTRACT_ID_BASE + 1;

        private readonly IBClient _iBClient;
        private readonly ITwsObjectFactory _twsObjectFactory;
        private bool _fundamentalsRequestActive = false;
        private string _symbol;

        public ContractManager(IBClient iBClient, ITwsObjectFactory twsObjectFactory)
        {
            _iBClient = iBClient;
            _twsObjectFactory = twsObjectFactory;
            _iBClient.FundamentalData += OnClientFundamentalData;            
        }

        private void OnClientFundamentalData(FundamentalsMessage message)
        {
            _fundamentalsRequestActive = false;
            Messenger.Default.Send(new FundamentalDataMessage(_symbol, FundamentalData.Parse(message.Data)));
        }

        public void RequestFundamentals(Contract contract, string reportType)
        {
            if (!_fundamentalsRequestActive)
            {
                _fundamentalsRequestActive = true;
                _symbol = contract.Symbol;
                _iBClient.ClientSocket.reqFundamentalData(FUNDAMENTALS_ID, contract, reportType, new List<TagValue>());
            }
            else
            {
                _fundamentalsRequestActive = false;
                _iBClient.ClientSocket.cancelFundamentalData(FUNDAMENTALS_ID);
            }
        }

        public async Task<IList<ContractDetails>> RequestDetailsAsync(Contract contract)
        {
            return await _twsObjectFactory.TwsController.GetContractAsync(contract);
        }
    }
}