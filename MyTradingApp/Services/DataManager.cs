namespace MyTradingApp.Services
{
    internal abstract class DataManager
    {
        protected IBClient ibClient;
        protected int currentTicker = 1;

        public DataManager(IBClient client)
        {
            ibClient = client;
        }

        public abstract void NotifyError(int requestId);

        public abstract void Clear();
    }
}