using System.Threading.Tasks;

namespace MyTradingApp.Core.Services
{
    public interface IRiskCalculationService
    {
        double RiskPerTrade
        {
            get;
        }

        Task RequestDataForCalculationAsync();

        void SetRiskMultiplier(double riskMultipier);
    }
}