namespace MyTradingApp.EventMessages
{
    public class StreamingChangedMessage
    {
        public StreamingChangedMessage(bool isStreaming)
        {
            IsStreaming = isStreaming;
        }

        public bool IsStreaming { get; }
    }
}
