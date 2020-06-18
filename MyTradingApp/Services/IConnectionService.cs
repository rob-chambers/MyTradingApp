using System;

namespace MyTradingApp.Services
{
    internal interface IConnectionService
    {
        event EventHandler<ConnectionStatusChangedEventArgs> ConnectionStatusChanged;
        event EventHandler<ClientError> ClientError;
        event EventHandler<ManagedAccountsEventArgs> ManagedAccounts;

        bool IsConnected { get; }
        void Connect();
        void Disconnect();
    }
}