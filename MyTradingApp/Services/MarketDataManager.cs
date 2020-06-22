using GalaSoft.MvvmLight.Messaging;
using IBApi;
using MyTradingApp.EventMessages;
using MyTradingApp.Messages;
using System.Collections.Generic;
using System.Diagnostics;

namespace MyTradingApp.Services
{
    internal class MarketDataManager : DataManager, IMarketDataManager
    {
        public const int TICK_ID_BASE = 10000000;
        private int _currentTicker = 1;
        private Dictionary<int, Contract> _activeRequests = new Dictionary<int, Contract>();

        public MarketDataManager(IBClient iBClient) : base(iBClient)
        {
            iBClient.TickGeneric += OnClientTickGeneric;
            iBClient.TickPrice += OnTickPrice;
        }

        private void OnTickPrice(TickPriceMessage msg)
        {
            if (msg.Field == TickType.LAST)
            {
                var symbol = _activeRequests[msg.RequestId].Symbol;
                Messenger.Default.Send(new TickPrice(symbol, msg.Price));
            }
        }

        private void OnClientTickGeneric(int tickerId, int field, double value)
        {
            Debug.WriteLine("OnClientTickGeneric for ticker id {0}: {1}:{2}",
                tickerId,
                field,
                value);
        }

        public void AddRequest(Contract contract, string genericTickList)
        {            
            var nextRequestId = TICK_ID_BASE + _currentTicker++;
            ibClient.ClientSocket.reqMktData(nextRequestId, contract, genericTickList, false, false, new List<TagValue>());
            _activeRequests.Add(nextRequestId, contract);
        }

        public void StopActiveRequests()
        {
            for (var i = 1; i < _currentTicker; i++)
            {
                ibClient.ClientSocket.cancelMktData(i + TICK_ID_BASE);
            }

            //if (clearTable)
            //    Clear();
        }

        public override void NotifyError(int requestId)
        {
            throw new System.NotImplementedException();
        }

        public override void Clear()
        {
            currentTicker = 1;
        }

        //private void checkToAddRow(int requestId)
        //{
        //    DataGridView grid = (DataGridView)uiControl;
        //    if (grid.Rows.Count < (requestId - TICK_ID_BASE))
        //    {
        //        grid.Rows.Add(GetIndex(requestId), 0);
        //        grid[DESCRIPTION_INDEX, GetIndex(requestId)].Value = Utils.ContractToString(activeRequests[GetIndex(requestId)]);
        //        grid[MARKET_DATA_TYPE_INDEX, GetIndex(requestId)].Value = MarketDataType.Real_Time.Name; // default
        //    }
        //}
    }
}
