using GalaSoft.MvvmLight.Messaging;
using IBApi;
using MyTradingApp.EventMessages;
using MyTradingApp.Messages;
using MyTradingApp.Models;
using System.Collections.Generic;

namespace MyTradingApp.Services
{
    internal class ContractManager : IContractManager
    {
        public const int CONTRACT_ID_BASE = 60000000;
        public const int CONTRACT_DETAILS_ID = CONTRACT_ID_BASE + 1;
        public const int FUNDAMENTALS_ID = CONTRACT_ID_BASE + 2;

        private readonly IBClient _iBClient;
        private bool _fundamentalsRequestActive = false;

        public ContractManager(IBClient iBClient)
        {
            _iBClient = iBClient;
            _iBClient.FundamentalData += OnClientFundamentalData;
        }

        private void OnClientFundamentalData(FundamentalsMessage message)
        {
            _fundamentalsRequestActive = false;
            Messenger.Default.Send(new FundamentalDataMessage(FundamentalData.Parse(message.Data)));
        }

        public void RequestFundamentals(Contract contract, string reportType)
        {
            if (!_fundamentalsRequestActive)
            {
                _fundamentalsRequestActive = true;
                _iBClient.ClientSocket.reqFundamentalData(FUNDAMENTALS_ID, contract, reportType, new List<TagValue>());
            }
            else
            {
                _fundamentalsRequestActive = false;
                _iBClient.ClientSocket.cancelFundamentalData(FUNDAMENTALS_ID);
            }
        }
    }
}