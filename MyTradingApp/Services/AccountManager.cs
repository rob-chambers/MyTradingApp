using MyTradingApp.Messages;
using System.Diagnostics;

namespace MyTradingApp.Services
{
    internal class AccountManager : IAccountManager
    {
        private const int ACCOUNT_ID_BASE = 50000000;
        private const int ACCOUNT_SUMMARY_ID = ACCOUNT_ID_BASE + 1;
        private const string ACCOUNT_SUMMARY_TAGS = "AccountType,NetLiquidation,TotalCashValue,SettledCash,AccruedCash,BuyingPower,EquityWithLoanValue,PreviousEquityWithLoanValue,"
             + "GrossPositionValue,ReqTEquity,ReqTMargin,SMA,InitMarginReq,MaintMarginReq,AvailableFunds,ExcessLiquidity,Cushion,FullInitMarginReq,FullMaintMarginReq,FullAvailableFunds,"
             + "FullExcessLiquidity,LookAheadNextChange,LookAheadInitMarginReq ,LookAheadMaintMarginReq,LookAheadAvailableFunds,LookAheadExcessLiquidity,HighestSeverity,DayTradesRemaining,Leverage";

        private readonly IBClient _iBClient;
        private bool _accountSummaryRequestActive;

        public AccountManager(IBClient iBClient)
        {
            _iBClient = iBClient;
        }

        public void RequestAccountSummary()
        {
            if (!_accountSummaryRequestActive)
            {
                _accountSummaryRequestActive = true;
                //accountSummaryGrid.Rows.Clear();
                _iBClient.ClientSocket.reqAccountSummary(ACCOUNT_SUMMARY_ID, "All", ACCOUNT_SUMMARY_TAGS);
            }
            else
            {
                _iBClient.ClientSocket.cancelAccountSummary(ACCOUNT_SUMMARY_ID);
            }
        }

        public void HandleAccountSummary(AccountSummaryMessage message)
        {
            // TODO: Output to UI
            Debug.WriteLine("Account summary info for account {0} in currency {1}: {2}={3}",
                message.Account, message.Currency, message.Tag, message.Value);
        }

        public void HandleAccountSummaryEnd()
        {
            _accountSummaryRequestActive = false;
        }
    }
}
