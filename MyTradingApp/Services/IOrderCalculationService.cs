using MyTradingApp.Models;
using System.Collections.Generic;

namespace MyTradingApp.Services
{
    public interface IOrderCalculationService
    {
        bool CanCalculate(string symbol);

        void SetLatestPrice(string symbol, double price);

        void SetHistoricalData(string symbol, ICollection<Bar> bars);

        double CalculateStandardDeviation(string symbol);

        double CalculateInitialStopLoss(string symbol);

        double GetCalculatedQuantity(string symbol);

        double GetEntryPrice(string symbol);

        void SetRiskPerTrade(double value);
    }
}