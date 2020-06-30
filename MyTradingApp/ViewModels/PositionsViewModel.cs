using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Messaging;
using MyTradingApp.EventMessages;
using MyTradingApp.Services;
using System;
using System.Collections.ObjectModel;
using System.Linq;

namespace MyTradingApp.ViewModels
{
    internal class PositionsViewModel : ViewModelBase
    {
        private readonly IMarketDataManager _marketDataManager;
        private readonly IAccountManager _accountManager;

        public ObservableCollection<PositionItem> Positions { get; } = new ObservableCollection<PositionItem>();

        public PositionsViewModel(IMarketDataManager marketDataManager, IAccountManager accountManager)
        {            
            Messenger.Default.Register<TickPrice>(this, HandleTickPriceMessage);
            Messenger.Default.Register<ConnectionChangingMessage>(this, HandleConnectionChangingMessage);
            Messenger.Default.Register<ConnectionChangedMessage>(this, HandleConnectionChangedMessage);
            Messenger.Default.Register<ExistingPositionsMessage>(this, HandlePositionsMessage);
            _marketDataManager = marketDataManager;
            _accountManager = accountManager;
        }

        private void HandleConnectionChangingMessage(ConnectionChangingMessage message)
        {
            if (message.IsConnecting)
            {
                return;
            }

            StopStreaming();
        }

        private void HandleConnectionChangedMessage(ConnectionChangedMessage message)
        {
            if (!message.IsConnected)
            {
                return;
            }

            _accountManager.RequestPositions();
        }

        private void StopStreaming()
        {
            foreach (var item in Positions)
            {
                _marketDataManager.StopPriceStreaming(item.Symbol.Code);
            }
        }

        private void HandlePositionsMessage(ExistingPositionsMessage message)
        {
            Positions.Clear();
            foreach (var item in message.Positions)
            {
                Positions.Add(item);
                if (item.Quantity > 0)
                {
                    _marketDataManager.RequestStreamingPrice(item.Contract);
                }                
            }
        }

        private void HandleTickPriceMessage(TickPrice tickPrice)
        {
            var positon = Positions.SingleOrDefault(p => p.Symbol.Code == tickPrice.Symbol);
            if (positon == null)
            {
                return;
            }            

            positon.Symbol.LatestPrice = tickPrice.Price;
            positon.ProfitLoss = positon.Quantity * (positon.Symbol.LatestPrice - positon.AvgPrice);
            positon.PercentageGainLoss = Math.Round((positon.Symbol.LatestPrice - positon.AvgPrice) / positon.AvgPrice * 100, 2);
        }
    }
}
