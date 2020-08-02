using MyTradingApp.Core.ViewModels;
using System.Threading.Tasks;

namespace MyTradingApp.Core.Services
{
    public interface IFindSymbolService
    {
        Task<FindCommandResultsModel> IssueFindSymbolRequestAsync(NewOrderViewModel order);
    }
}
