namespace MyTradingApp.Core.EventMessages
{
    public class ConnectionChangedMessage
    {
        public ConnectionChangedMessage(bool isConnected)
        {
            IsConnected = isConnected;
        }

        public bool IsConnected { get; }
    }
}
