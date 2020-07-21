using MyTradingApp.Domain;

namespace MyTradingApp.Services
{
    public interface IOrderCalculationService
    {
        bool CanCalculate(string symbol);

        void SetLatestPrice(string symbol, double price);

        void SetHistoricalData(string symbol, BarCollection bars);

        double CalculateStandardDeviation(string symbol);

        double CalculateInitialStopLoss(string symbol, Direction direction);

        ushort GetCalculatedQuantity(string symbol, Direction direction);

        double GetEntryPrice(string symbol, Direction direction);

        void SetRiskPerTrade(double value);
    }
}