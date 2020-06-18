using MyTradingApp.Messages;

namespace MyTradingApp.Services
{
    internal interface IAccountManager
    {
        void RequestAccountSummary();
        void HandleAccountSummary(AccountSummaryMessage obj);
        void HandleAccountSummaryEnd();
    }
}