using System.Threading.Tasks;

namespace MyTradingApp.Core.Services
{
    public interface IExchangeRateService
    {
        Task<double> GetExchangeRateAsync();
    }
}