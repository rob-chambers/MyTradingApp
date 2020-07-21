using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Messaging;
using MyTradingApp.EventMessages;
using MyTradingApp.Models;

namespace MyTradingApp.ViewModels
{
    public class DetailsViewModel : ViewModelBase
    {
        private RelayCommand _closeDetailsCommand;
        private OrderItem _selection;

        public RelayCommand CloseDetailsCommand => _closeDetailsCommand ?? (_closeDetailsCommand = new RelayCommand(CloseDetails));

        public OrderItem Selection
        {
            get => _selection;
            set => Set(ref _selection, value);
        }

        private void CloseDetails()
        {
            Messenger.Default.Send(new DetailsPanelClosedMessage());
        }
    }
}
