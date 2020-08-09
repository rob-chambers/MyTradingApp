using MyTradingApp.Core.ViewModels;
using MyTradingApp.Domain;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MyTradingApp.Core.Services
{
    public interface IAccountManager
    {
        Task<AccountSummary> RequestAccountSummaryAsync();

        Task<IEnumerable<PositionItem>> RequestPositionsAsync();
    }
}