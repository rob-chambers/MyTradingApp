using MyTradingApp.Models;
using System.Collections.Generic;

namespace MyTradingApp.Services
{
    public interface IOrderCalculationService
    {
        bool CanCalculate { get; }

        void SetLatestPrice(double price);

        void SetHistoricalData(ICollection<Bar> bars);

        double CalculateStandardDeviation();

        double CalculateInitialStopLoss();

        double GetCalculatedQuantity();

        double GetEntryPrice();

        void SetRiskPerTrade(double value);
    }
}