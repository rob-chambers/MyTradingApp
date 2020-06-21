using MyTradingApp.Models;
using System.Collections.Generic;

namespace MyTradingApp.Services
{
    public interface IOrderCalculationService
    {
        void SetHistoricalData(ICollection<Bar> bars);
        double CalculateStandardDeviation();
        double CalculateInitialStopLoss();
        double GetCalculatedQuantity();
    }
}