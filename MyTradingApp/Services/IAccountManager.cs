using MyTradingApp.Messages;
using MyTradingApp.Models;
using System;

namespace MyTradingApp.Services
{
    internal interface IAccountManager
    {
        event EventHandler<AccountSummaryEventArgs> AccountSummary;
        void RequestAccountSummary();
        void HandleAccountSummary(AccountSummaryMessage obj);
        void HandleAccountSummaryEnd();
    }
}