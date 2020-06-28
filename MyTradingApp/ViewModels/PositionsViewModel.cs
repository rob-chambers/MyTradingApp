using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Messaging;
using MyTradingApp.EventMessages;
using MyTradingApp.Models;
using System.Collections.ObjectModel;

namespace MyTradingApp.ViewModels
{
    internal class PositionsViewModel : ViewModelBase
    {
        public ObservableCollection<PositionItem> Positions { get; } = new ObservableCollection<PositionItem>();

        public PositionsViewModel()
        {
            Messenger.Default.Register<ExistingPositionsMessage>(this, HandlePositionsMessage);
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
        }

        private void HandlePositionsMessage(ExistingPositionsMessage message)
        {
            Positions.Clear();
            foreach (var item in message.Positions)
            {
                Positions.Add(item);
            }
        }
    }
}
