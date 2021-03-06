﻿using MyTradingApp.Domain;

namespace MyTradingApp.Core.ViewModels
{
    internal class RiskCalculationServiceData
    {
        public RiskCalculationServiceData(double exchangeRate, AccountSummary accountSummary)
        {
            ExchangeRate = exchangeRate;
            AccountSummary = accountSummary;
        }

        public double ExchangeRate { get; }

        public AccountSummary AccountSummary { get; }
    }
}
