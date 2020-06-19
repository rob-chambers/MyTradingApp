using IBApi;
using MyTradingApp.Messages;
using System.Collections.Generic;
using System.Diagnostics;

namespace MyTradingApp.Services
{
    internal class MarketDataManager : IMarketDataManager
    {
        public const int TICK_ID_BASE = 10000000;
        private readonly IBClient _iBClient;
        private int _currentTicker = 1;

        private List<Contract> _activeRequests = new List<Contract>();

        public MarketDataManager(IBClient iBClient)
        {
            _iBClient = iBClient;
            _iBClient.TickGeneric += OnClientTickGeneric;
            _iBClient.TickPrice += OnTickPrice;
        }

        private void OnTickPrice(TickPriceMessage msg)
        {
            Debug.WriteLine("OnTickPrice for request {0}: {1}:",
                msg.RequestId,
                TickType.getField(msg.Field), msg.Price,
                msg.Price);

            /*
            addTextToBox("Tick Price. Ticker Id:" + msg.RequestId + ", Type: " + TickType.getField(msg.Field) + 
            ", Price: " + msg.Price + ", Pre-Open: " + msg.Attribs.PreOpen + "\n");

            if (msg.RequestId < OptionsManager.OPTIONS_ID_BASE)
            {
                if (marketDataManager.IsUIUpdateRequired(msg))
                    marketDataManager.UpdateUI(msg);
            }
            else
            {
                HandleTickMessage(msg);
            }
            */
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
            _activeRequests.Add(contract);
            var nextRequestId = TICK_ID_BASE + _currentTicker++;
            //checkToAddRow(nextReqId);
            _iBClient.ClientSocket.reqMktData(nextRequestId, contract, genericTickList, false, false, new List<TagValue>());

            //if (!uiControl.Visible)
            //    uiControl.Visible = true;
        }


        public void StopActiveRequests()
        {
            for (var i = 1; i < _currentTicker; i++)
            {
                _iBClient.ClientSocket.cancelMktData(i + TICK_ID_BASE);
            }

            //if (clearTable)
            //    Clear();
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
