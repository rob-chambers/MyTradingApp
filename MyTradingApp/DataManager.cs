using System.Windows.Controls;

namespace MyTradingApp
{
    abstract class DataManager
    {
        protected Control uiControl;
        protected IBClient ibClient;
        protected int currentTicker = 1;

        public DataManager(IBClient client, Control dataGrid)
        {
            ibClient = client;
            uiControl = dataGrid;
        }

        public abstract void NotifyError(int requestId);

        public abstract void Clear();
    }
}