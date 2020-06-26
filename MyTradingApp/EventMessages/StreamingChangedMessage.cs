namespace MyTradingApp.EventMessages
{
    internal class StreamingChangedMessage
    {
        public StreamingChangedMessage(bool isStreaming)
        {
            IsStreaming = isStreaming;
        }

        public bool IsStreaming { get; }
    }
}
