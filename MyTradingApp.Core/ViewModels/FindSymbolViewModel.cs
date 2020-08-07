using GalaSoft.MvvmLight.Messaging;
using IBApi;
using MyTradingApp.Core.Services;
using MyTradingApp.Core.Utils;
using MyTradingApp.Domain;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;

namespace MyTradingApp.Core.ViewModels
{
    public class FindSymbolViewModel : DispatcherViewModel
    {
        private readonly IFindSymbolService _findSymbolService;
        private AsyncCommand _findCommand;
        private bool _isBusy;
        private string _findCommandCaption = FindButtonCaptions.Default;

        #region Nested Classes

        public static class FindButtonCaptions
        {
            public const string Default = "Find";
            public const string Finding = "Finding...";
        }

        #endregion Nested Classes

        public FindSymbolViewModel(
            IDispatcherHelper dispatcherHelper,
            IQueueProcessor queueProcessor,
            IFindSymbolService findSymbolService,
            OrdersListViewModel ordersListViewModel)
            : base(dispatcherHelper, queueProcessor)
        {
            _findSymbolService = findSymbolService;
            OrdersListViewModel = ordersListViewModel;
            Symbol.PropertyChanged += OnSymbolPropertyChanged;
        }

        public Symbol Symbol { get; } = new Symbol();

        public bool IsBusy
        {
            get => _isBusy;
            private set => Set(ref _isBusy, value);
        }

        public string FindCommandCaption
        {
            get => _findCommandCaption;
            set => Set(ref _findCommandCaption, value);
        }

        public OrdersListViewModel OrdersListViewModel { get; }

        public AsyncCommand FindCommand
        {
            get
            {
                return _findCommand ?? (_findCommand = new AsyncCommand(
                    DispatcherHelper,
                    () => FindSymbolAndProcessAsync(),
                    () => !IsBusy && !string.IsNullOrEmpty(Symbol.Code)));
            }
        }

        private async Task FindSymbolAndProcessAsync()
        {
            if (OrdersListViewModel.Orders.Any(o => o.Symbol.Code == Symbol.Code))
            {
                var message = "There is already an order for this symbol.";
                Messenger.Default.Send(new NotificationMessage<NotificationType>(NotificationType.Error, message));
                return;
            }

            IsBusy = true;
            FindCommandCaption = FindButtonCaptions.Finding;
            FindCommand.RaiseCanExecuteChanged();

            try
            {
                var contract = Symbol.ToContract();
                var results = await _findSymbolService.IssueFindSymbolRequestAsync(contract).ConfigureAwait(false);
                if (results.Details == null)
                {
                    var message = "This symbol was not found.";
                    Messenger.Default.Send(new NotificationMessage<NotificationType>(NotificationType.Warning, message));
                    return;
                }

                var details = results.Details.First();
                Symbol.IsFound = true;
                Symbol.LatestPrice = results.LatestPrice;
                Symbol.Name = details.LongName;
                Symbol.MinTick = details.MinTick;

                OrdersListViewModel.AddOrder(Symbol, results);
            }
            finally
            {
                IsBusy = false;
                FindCommandCaption = FindButtonCaptions.Default;
                DispatcherHelper.InvokeOnUiThread(() => FindCommand.RaiseCanExecuteChanged());
            }
        }

        private void OnSymbolPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName != nameof(Symbol.Code))
            {
                return;
            }

            DispatcherHelper.InvokeOnUiThread(() => FindCommand.RaiseCanExecuteChanged());
        }
    }
}