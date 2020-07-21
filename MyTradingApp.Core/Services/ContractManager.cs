using GalaSoft.MvvmLight.Messaging;
using IBApi;
using MyTradingApp.Domain;
using MyTradingApp.EventMessages;
using MyTradingApp.Messages;
using MyTradingApp.Models;
using Serilog;
using System.Collections.Generic;

namespace MyTradingApp.Services
{
    public class ContractManager : IContractManager
    {
        public const int CONTRACT_ID_BASE = 60000000;        
        public const int FUNDAMENTALS_ID = CONTRACT_ID_BASE + 1;
        public const int CONTRACT_DETAILS_ID = CONTRACT_ID_BASE + 1000;

        private readonly IBClient _iBClient;
        private readonly Dictionary<int, ContractDetailsMessage> _contractDetailsMessage = new Dictionary<int, ContractDetailsMessage>();
        private bool _fundamentalsRequestActive = false;
        private string _symbol;
        private int _latestRequestId;

        public ContractManager(IBClient iBClient)
        {
            _iBClient = iBClient;
            _iBClient.FundamentalData += OnClientFundamentalData;
            _iBClient.ContractDetails += OnContractDetails;
            _iBClient.ContractDetailsEnd += OnContractDetailsEnd;
        }

        private void OnContractDetails(ContractDetailsMessage message)
        {
            _contractDetailsMessage.Add(message.RequestId, message);
        }

        private void OnContractDetailsEnd(int requestId)
        {
            if (!_contractDetailsMessage.ContainsKey(requestId))
            {
                Log.Debug("Unexpected scenario in {0}, requestId = {1}", nameof(OnContractDetailsEnd), requestId);
                return;
            }

            Messenger.Default.Send(new ContractDetailsEventMessage(_contractDetailsMessage[requestId].ContractDetails));
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

        public void RequestDetails(Contract contract)
        {
            var nextRequestId = CONTRACT_DETAILS_ID + _latestRequestId++;
            _iBClient.ClientSocket.reqContractDetails(nextRequestId, contract);
        }
    }
}