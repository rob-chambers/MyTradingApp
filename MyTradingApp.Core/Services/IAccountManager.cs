using MyTradingApp.EventMessages;
using MyTradingApp.ViewModels;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MyTradingApp.Services
{
    public interface IAccountManager
    {
        Task<AccountSummaryCompletedMessage> RequestAccountSummaryAsync();

        Task<IEnumerable<PositionItem>> RequestPositionsAsync();        
    }
}