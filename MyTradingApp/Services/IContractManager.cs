using IBApi;

namespace MyTradingApp.Services
{
    public interface IContractManager
    {
        void RequestFundamentals(Contract contract, string reportType);
    }
}