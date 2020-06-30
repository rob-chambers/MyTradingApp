namespace MyTradingApp.EventMessages
{
    internal class ConnectionChangedMessage
    {
        public ConnectionChangedMessage(bool isConnected)
        {
            IsConnected = isConnected;
        }

        public bool IsConnected { get; }
    }
}
