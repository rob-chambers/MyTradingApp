using MyTradingApp.Models;
using System;

namespace MyTradingApp.Services
{
    public interface IConnectionService
    {
        event EventHandler<ClientError> ClientError;

        bool IsConnected { get; }
        void Connect();
        void Disconnect();
    }
}