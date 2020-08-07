using MyTradingApp.Domain;
using System;
using System.Threading.Tasks;

namespace MyTradingApp.Core.Services
{
    public interface IConnectionService
    {
        event EventHandler<ClientError> ClientError;

        bool IsConnected { get; }

        Task ConnectAsync();

        Task DisconnectAsync();
    }
}