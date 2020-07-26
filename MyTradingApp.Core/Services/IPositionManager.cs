﻿using AutoFinance.Broker.InteractiveBrokers.EventArgs;
using IBApi;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MyTradingApp.Services
{
    public interface IPositionManager
    {
        Task<IEnumerable<OpenOrderEventArgs>> RequestOpenOrdersAsync();
        
        Task UpdateStopOrderAsync(Contract contract, Order order);
    }
}