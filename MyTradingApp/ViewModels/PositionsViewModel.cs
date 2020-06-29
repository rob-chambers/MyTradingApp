using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Messaging;
using MyTradingApp.EventMessages;
using MyTradingApp.Models;
using MyTradingApp.Services;
using System.Collections.ObjectModel;
using System.Linq;

namespace MyTradingApp.ViewModels
{
    internal class PositionsViewModel : ViewModelBase
    {
        private readonly IMarketDataManager _marketDataManager;

        public ObservableCollection<PositionItem> Positions { get; } = new ObservableCollection<PositionItem>();

        public PositionsViewModel(IMarketDataManager marketDataManager)
        {
            Messenger.Default.Register<ExistingPositionsMessage>(this, HandlePositionsMessage);
            Messenger.Default.Register<TickPrice>(this, HandleTickPriceMessage);
            Messenger.Default.Register<ConnectionChangingMessage>(this, HandleConnectionChangingMessage);
            Positions.Add(new PositionItem
            {
                AvgPrice = 11.03,
                ProfitLoss = 231.56,
                Quantity = 233,
                Symbol = new Symbol
                {
                    Code = "CAT",
                    Name = "Caterpillar",
                    LatestPrice = 11.87,
                }
            });
            _marketDataManager = marketDataManager;
        }

        private void HandleConnectionChangingMessage(ConnectionChangingMessage message)
        {
            if (message.IsConnecting)
            {
                return;
            }

            StopStreaming();
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
        }
    }
}
