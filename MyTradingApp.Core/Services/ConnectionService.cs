using AutoFinance.Broker.InteractiveBrokers.Controllers;
using AutoFinance.Broker.InteractiveBrokers.EventArgs;
using GalaSoft.MvvmLight.Messaging;
using MyTradingApp.Domain;
using MyTradingApp.EventMessages;
using Serilog;
using System;
using System.Threading.Tasks;

namespace MyTradingApp.Services
{
    public class ConnectionService : IConnectionService
    {
        private readonly ITwsObjectFactory _twsObjectFactory;
        private bool _isConnected;

        public event EventHandler<ClientError> ClientError;

        public ConnectionService(ITwsObjectFactory twsObjectFactory)
        {
            _twsObjectFactory = twsObjectFactory;
            _twsObjectFactory.TwsCallbackHandler.ErrorEvent += OnClientError;
            _twsObjectFactory.TwsCallbackHandler.ConnectionClosedEvent += OnClientConnectionClosed;
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

        private void OnClientError(object sender, ErrorEventArgs e)
        {
            ClientError?.Invoke(this, new ClientError(e.Id, e.ErrorCode, e.ErrorMessage));
        }

        private void OnClientConnectionClosed(object sender, EventArgs e)
        {
            IsConnected = false;
            Messenger.Default.Send(new ConnectionChangedMessage(false));
        }

        public async Task ConnectAsync()
        {
            if (IsConnected)
                return;

            try
            {
                Log.Information("Connecting to TWS");
                var twsController = _twsObjectFactory.TwsControllerBase;
                await twsController.EnsureConnectedAsync();
                IsConnected = true;
                Messenger.Default.Send(new ConnectionChangedMessage(true));
            }
            catch (Exception ex)
            {
                throw new ApplicationException("Failed to connect", ex);
            } 
        }

        public async Task DisconnectAsync()
        {
            Messenger.Default.Send(new ConnectionChangingMessage(false));
            await _twsObjectFactory.TwsController.DisconnectAsync();
        }
    }
}