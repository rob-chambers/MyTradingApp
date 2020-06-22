using GalaSoft.MvvmLight.Messaging;
using IBApi;
using MyTradingApp.Models;
using System;
using System.Threading;

namespace MyTradingApp.Services
{
    internal class ConnectionService : IConnectionService
    {        
        private readonly IBClient _ibClient;
        private readonly EReaderSignal _signal;
        private bool _isConnected;

        public event EventHandler<ClientError> ClientError;

        public ConnectionService(IBClient iBClient, EReaderSignal signal)
        {
            _ibClient = iBClient;
            _signal = signal;
            _ibClient.ConnectionClosed += OnClientConnectionClosed;
            _ibClient.Error += OnClientError;
            //_ibClient.ManagedAccounts += message => Messenger.Default.Send(new ManagedAccountsEventArgs(message));
        }        

        private void OnClientError(int id, int errorCode, string message, Exception ex)
        {
            ClientError?.Invoke(this, new ClientError(id, errorCode, message, ex));
        }

        private void OnClientConnectionClosed() => IsConnected = false;

        public bool IsConnected
        {
            get => _isConnected;
            private set
            {
                if (_isConnected == value) return;
                _isConnected = value;
                Messenger.Default.Send(new ConnectionStatusChangedMessage(value));
            }
        }

        public void Connect()
        {
            if (IsConnected)
                return;

            int port;
            var host = "127.0.0.1";
            try
            {
                port = 7497; // 7496 for live account
                _ibClient.ClientId = 1; // Assume a single client
                _ibClient.ClientSocket.eConnect(host, port, _ibClient.ClientId);

                var reader = new EReader(_ibClient.ClientSocket, _signal);

                reader.Start();

                new Thread(() =>
                {
                    while (_ibClient.ClientSocket.IsConnected())
                    {
                        _signal.waitForSignal();
                        reader.processMsgs();
                    }
                })
                {
                    IsBackground = true
                }.Start();

                IsConnected = true;
            }
            catch (Exception ex)
            {
                throw new ApplicationException("Failed to connect", ex);
            }
        }

        public void Disconnect()
        {
            IsConnected = false;
            _ibClient.ClientSocket.eDisconnect();
        }
    }
}
