using MyTradingApp.EventMessages;

namespace MyTradingApp.Core.ViewModels
{
    internal class ApiInitialDataViewModel
    {
        public ApiInitialDataViewModel(double exchangeRate, AccountSummaryCompletedMessage accountSummary)
        {
            ExchangeRate = exchangeRate;
            AccountSummary = accountSummary;
        }

        public double ExchangeRate { get; }

        public AccountSummaryCompletedMessage AccountSummary { get; }
    }
}
