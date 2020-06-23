using MyTradingApp.Models;
using System.Collections.Generic;

namespace MyTradingApp.Services
{
    public interface IOrderCalculationService
    {
        void SetLatestPrice(double price);
        void SetHistoricalData(ICollection<Bar> bars);
        double CalculateStandardDeviation();
        double CalculateInitialStopLoss();
        double GetCalculatedQuantity();
        double GetEntryPrice();
        void SetExchangeRate(double rate);
    }
}