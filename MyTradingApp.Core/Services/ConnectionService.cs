using AutoFinance.Broker.InteractiveBrokers.Controllers;
using GalaSoft.MvvmLight.Messaging;
using IBApi;
using MyTradingApp.Domain;
using MyTradingApp.EventMessages;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace MyTradingApp.Services
{
    public class ConnectionService : IConnectionService
    {
        private readonly IBClient _ibClient;
        private readonly EReaderSignal _signal;
        private readonly ITwsObjectFactory _twsObjectFactory;
        private bool _isConnected;

        public event EventHandler<ClientError> ClientError;

        public ConnectionService(IBClient iBClient, EReaderSignal signal, ITwsObjectFactory twsObjectFactory)
        {
            _ibClient = iBClient;
            _signal = signal;
            _twsObjectFactory = twsObjectFactory;
            _ibClient.ConnectionClosed += OnClientConnectionClosed;
            _ibClient.Error += OnClientError;
        }

        private void OnClientError(int id, int errorCode, string message, Exception ex)
        {
            ClientError?.Invoke(this, new ClientError(id, errorCode, message, ex));
        }

        private void OnClientConnectionClosed()
        {
            IsConnected = false;
            Messenger.Default.Send(new ConnectionChangedMessage(false));
        }

        public bool IsConnected
        {
            get => _isConnected;
            private set
            {
                if (_isConnected == value) return;
                _isConnected = value;
            }
        }

        public void Connect()
        {
            if (IsConnected)
                return;

            Messenger.Default.Send(new ConnectionChangingMessage(true));

            int port;
            var host = "127.0.0.1";
            try
            {
                port = 7497; // 7496 for live account
                _ibClient.ClientId = BrokerConstants.ClientId;
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

                IsConnected = _ibClient.ClientSocket.IsConnected();
                if (IsConnected)
                {
                    Messenger.Default.Send(new ConnectionChangedMessage(true));
                }
            }
            catch (Exception ex)
            {
                throw new ApplicationException("Failed to connect", ex);
            }
        }

        public async Task ConnectAsync()
        {
            var twsController = _twsObjectFactory.TwsControllerBase;
            await twsController.EnsureConnectedAsync();
        }

        public void Disconnect()
        {
            Messenger.Default.Send(new ConnectionChangingMessage(false));
            _ibClient.ClientSocket.eDisconnect();
        }

        public async Task DisconnectAsync()
        {
            await _twsObjectFactory.TwsController.DisconnectAsync();
        }
    }
}