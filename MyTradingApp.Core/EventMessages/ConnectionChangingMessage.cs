namespace MyTradingApp.Core.EventMessages
{
    public class ConnectionChangingMessage
    {
        public ConnectionChangingMessage(bool isConnecting)
        {
            IsConnecting = isConnecting;
        }

        public bool IsConnecting { get; }
    }
}
