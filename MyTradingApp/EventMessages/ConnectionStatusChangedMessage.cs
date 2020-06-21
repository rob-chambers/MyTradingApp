namespace MyTradingApp
{
    public class ConnectionStatusChangedMessage
    {
        public ConnectionStatusChangedMessage(bool isConnected)
        {
            IsConnected = isConnected;
        }

        public bool IsConnected { get; }
    }
}
