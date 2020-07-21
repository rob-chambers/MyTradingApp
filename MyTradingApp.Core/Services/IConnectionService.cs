using MyTradingApp.Domain;
using System;
using System.Threading.Tasks;

namespace MyTradingApp.Services
{
    public interface IConnectionService
    {
        event EventHandler<ClientError> ClientError;

        bool IsConnected { get; }

        void Connect();

        void Disconnect();

        Task ConnectAsync();

        Task DisconnectAsync();
    }
}