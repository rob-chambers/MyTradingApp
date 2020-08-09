using MyTradingApp.Domain;

namespace MyTradingApp.Core.EventMessages
{
    public class AccountSummaryMessage
    {
        public AccountSummaryMessage(AccountSummary summary)
        {
            AccountSummary = summary;
        }

        public AccountSummary AccountSummary
        {
            get;
        }
    }
}
