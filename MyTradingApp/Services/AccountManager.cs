using GalaSoft.MvvmLight.Messaging;
using MyTradingApp.Messages;
using System;
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

        private static class AccountSummaryTags
        {
            public const string BuyingPower = "BuyingPower";
            public const string AvailableFunds = "AvailableFunds";
        }

        private readonly IBClient _iBClient;
        private bool _accountSummaryRequestActive;
        private int _dataCount = 0;
        private Dictionary<string, string> _accountData = new Dictionary<string, string>();

        public AccountManager(IBClient iBClient)
        {
            _iBClient = iBClient;
        }

        public void RequestAccountSummary()
        {
            if (!_accountSummaryRequestActive)
            {
                _dataCount = 0;
                _accountData.Clear();
                _accountSummaryRequestActive = true;
                var tags = AccountSummaryTags.BuyingPower + "," + AccountSummaryTags.AvailableFunds;
                _iBClient.ClientSocket.reqAccountSummary(ACCOUNT_SUMMARY_ID, "All", tags);
            }
            else
            {
                _iBClient.ClientSocket.cancelAccountSummary(ACCOUNT_SUMMARY_ID);
            }
        }

        public void HandleAccountSummary(Messages.AccountSummaryMessage msg)
        {
            _accountData.Add(msg.Tag, msg.Value);
            _dataCount++;            
            if (_dataCount <= 1)
            {                
                return;
            }

            // We have both data fields - raise event now
            var message = new EventMessages.AccountSummaryMessage();
            if (_accountData.ContainsKey(AccountSummaryTags.AvailableFunds))
            {
                message.AvailableFunds = double.Parse(_accountData[AccountSummaryTags.AvailableFunds]);
            }

            if (_accountData.ContainsKey(AccountSummaryTags.BuyingPower))
            {
                message.BuyingPower = double.Parse(_accountData[AccountSummaryTags.BuyingPower]);
            }

            Messenger.Default.Send(message);
        }

        public void HandleAccountSummaryEnd()
        {
            _accountSummaryRequestActive = false;
        }
    }
}
