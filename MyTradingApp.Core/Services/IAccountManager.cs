using MyTradingApp.Core.EventMessages;
using MyTradingApp.Core.ViewModels;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MyTradingApp.Core.Services
{
    public interface IAccountManager
    {
        Task<AccountSummaryCompletedMessage> RequestAccountSummaryAsync();

        Task<IEnumerable<PositionItem>> RequestPositionsAsync();
    }
}