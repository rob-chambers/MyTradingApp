namespace MyTradingApp.EventMessages
{
    internal class ConnectionChangingMessage
    {
        public ConnectionChangingMessage(bool isConnecting)
        {
            IsConnecting = isConnecting;
        }

        public bool IsConnecting { get; }
    }
}
