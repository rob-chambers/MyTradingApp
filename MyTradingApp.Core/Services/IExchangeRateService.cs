using System.Threading.Tasks;

namespace MyTradingApp.Services
{
    public interface IExchangeRateService
    {
        Task<double> GetExchangeRateAsync();
    }
}