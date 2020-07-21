using MyTradingApp.Messages;

namespace MyTradingApp.Services
{
    public interface IAccountManager
    {
        void RequestAccountSummary();

        void HandleAccountSummary(AccountSummaryMessage message);

        void HandleAccountSummaryEnd();

        void RequestPositions();
    }
}