using GalaSoft.MvvmLight.Messaging;
using MyTradingApp.EventMessages;
using MyTradingApp.Messages;
using MyTradingApp.Models;
using MyTradingApp.ViewModels;
using System.Collections.Generic;

namespace MyTradingApp.Services
{
    internal class AccountManager : IAccountManager
    {
        private const int ACCOUNT_ID_BASE = 50000000;
        private const int ACCOUNT_SUMMARY_ID = ACCOUNT_ID_BASE + 1;

        private const string ACCOUNT_SUMMARY_TAGS = "AccountType,NetLiquidation,TotalCashValue,SettledCash,AccruedCash,BuyingPower,EquityWithLoanValue,PreviousEquityWithLoanValue,"
             + "GrossPositionValue,ReqTEquity,ReqTMargin,SMA,InitMarginReq,MaintMarginReq,AvailableFunds,ExcessLiquidity,Cushion,FullInitMarginReq,FullMaintMarginReq,FullAvailableFunds,"
             + "FullExcessLiquidity,LookAheadNextChange,LookAheadInitMarginReq ,LookAheadMaintMarginReq,LookAheadAvailableFunds,LookAheadExcessLiquidity,HighestSeverity,DayTradesRemaining,Leverage";

        public static class AccountSummaryTags
        {
            public const string BuyingPower = "BuyingPower";
            public const string NetLiquidation = "NetLiquidation";
        }

        private readonly IBClient _iBClient;
        private bool _accountSummaryRequestActive;
        private int _dataCount = 0;
        private Dictionary<string, string> _accountData = new Dictionary<string, string>();
        private List<PositionItem> _positions;

        public AccountManager(IBClient iBClient)
        {
            _iBClient = iBClient;
            _iBClient.Position += HandlePositionMessage;
            _iBClient.PositionEnd += OnClientPositionEnd;
        }

        public void RequestAccountSummary()
        {
            if (!_accountSummaryRequestActive)
            {
                _dataCount = 0;
                _accountData.Clear();
                _accountSummaryRequestActive = true;
                var tags = AccountSummaryTags.BuyingPower + "," + AccountSummaryTags.NetLiquidation;
                _iBClient.ClientSocket.reqAccountSummary(ACCOUNT_SUMMARY_ID, "All", tags);
            }
            else
            {
                _iBClient.ClientSocket.cancelAccountSummary(ACCOUNT_SUMMARY_ID);
            }
        }

        public void HandleAccountSummary(AccountSummaryMessage msg)
        {
            if (_accountData.ContainsKey(msg.Tag))
            {
                return;
            }

            _accountData.Add(msg.Tag, msg.Value);
            _dataCount++;
            if (_dataCount <= 1)
            {
                return;
            }

            // We have both data fields - raise event now
            var message = new AccountSummaryCompletedMessage();
            if (_accountData.ContainsKey(AccountSummaryTags.NetLiquidation))
            {
                message.NetLiquidation = double.Parse(_accountData[AccountSummaryTags.NetLiquidation]);
            }

            if (_accountData.ContainsKey(AccountSummaryTags.BuyingPower))
            {
                message.BuyingPower = double.Parse(_accountData[AccountSummaryTags.BuyingPower]);
            }

            message.AccountId = msg.Account;

            Messenger.Default.Send(message);
        }

        public void HandleAccountSummaryEnd()
        {
            _accountSummaryRequestActive = false;
        }

        public void RequestPositions()
        {
            _positions = new List<PositionItem>();
            _iBClient.ClientSocket.reqPositions();
        }

        private void OnClientPositionEnd()
        {
            Messenger.Default.Send(new ExistingPositionsMessage(_positions));
        }

        public void HandlePositionMessage(PositionMessage positionMessage)
        {
            if (positionMessage.Contract.SecType != BrokerConstants.Stock)
            {
                return;
            }

            _positions.Add(new PositionItem
            {
                AvgPrice = positionMessage.AverageCost,
                Quantity = positionMessage.Position,
                Symbol = new Symbol
                {
                    Code = positionMessage.Contract.Symbol
                }
            });           
        }
    }
}