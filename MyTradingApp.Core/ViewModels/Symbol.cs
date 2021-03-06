﻿using GalaSoft.MvvmLight;
using IBApi;
using MyTradingApp.Core.Utils;
using MyTradingApp.Domain;

namespace MyTradingApp.Core.ViewModels
{
    public class Symbol : ViewModelBase
    {
        private string _code;
        private Exchange _exchange;
        private string _name;
        private double _latestPrice;
        private bool _isFound;
        private double _latestHigh;
        private double _latestLow = double.MaxValue;
        private double _minTick;

        public string Code
        {
            get => _code;
            set => Set(ref _code, value?.ToUpperInvariant().Trim());
        }

        public Exchange Exchange
        {
            get => _exchange;
            set => Set(ref _exchange, value);
        }

        public string Name
        {
            get => _name;
            set => Set(ref _name, value);
        }

        public double LatestPrice
        {
            get => _latestPrice;
            set
            {
                Set(ref _latestPrice, value);
                CalculateLatestHigh();
                CalculateLatestLow();
            }
        }

        public double LatestHigh
        {
            get => _latestHigh;
            set
            {
                Set(ref _latestHigh, value);
            }
        }

        public double LatestLow
        {
            get => _latestLow;
            set
            {
                Set(ref _latestLow, value);
            }
        }

        public bool IsFound
        {
            get => _isFound;
            set => Set(ref _isFound, value);
        }

        public double MinTick
        {
            get => _minTick;
            set => Set(ref _minTick, value);
        }

        public Contract ToContract()
        {
            var contract = new Contract
            {
                Symbol = Code,
                SecType = BrokerConstants.Stock,
                Exchange = BrokerConstants.Routers.Smart,
                PrimaryExch = IbClientRequestHelper.MapExchange(Exchange),
                Currency = BrokerConstants.UsCurrency,
                LastTradeDateOrContractMonth = string.Empty,
                Strike = 0,
                Multiplier = string.Empty,
                LocalSymbol = string.Empty
            };

            return contract;
        }

        private void CalculateLatestHigh()
        {
            if (_latestPrice > _latestHigh)
            {
                LatestHigh = _latestPrice;
            }
        }

        private void CalculateLatestLow()
        {
            if (_latestPrice < _latestLow)
            {
                LatestLow = _latestPrice;
            }
        }

        public override string ToString()
        {
            return Code;
        }
    }
}