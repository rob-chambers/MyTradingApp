using IBApi;
using MyTradingApp.Core.ViewModels;
using System.Threading.Tasks;

namespace MyTradingApp.Core.Services
{
    public interface IFindSymbolService
    {
        Task<FindCommandResultsModel> IssueFindSymbolRequestAsync(Contract contract);
    }
}
