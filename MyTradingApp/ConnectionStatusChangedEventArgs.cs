using System;

namespace MyTradingApp
{
    public class ConnectionStatusChangedEventArgs : EventArgs
    {
        public ConnectionStatusChangedEventArgs(bool isConnected)
        {
            IsConnected = isConnected;
        }

        public bool IsConnected { get; set; }
    }
}
