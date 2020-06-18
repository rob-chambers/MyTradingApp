using System;

namespace MyTradingApp
{
    internal class ConnectionStatusChangedEventArgs : EventArgs
    {
        public ConnectionStatusChangedEventArgs(bool isConnected)
        {
            IsConnected = isConnected;
        }

        public bool IsConnected { get; set; }
    }
}
