using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Messaging;
using MahApps.Metro.IconPacks;
using MyTradingApp.Core.EventMessages;
using MyTradingApp.Core.Services;
using MyTradingApp.Core.Utils;
using MyTradingApp.Domain;
using MyTradingApp.Messages;
using Serilog;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;

namespace MyTradingApp.Core.ViewModels
{
    public class MainViewModel : DispatcherViewModel
    {
        #region Fields
        public static bool IsUnitTesting = false;

        private const int MAX_LINES_IN_MESSAGE_BOX = 200;

        private const int REDUCED_LINES_IN_MESSAGE_BOX = 100;

        private readonly IConnectionService _connectionService;
        private readonly List<string> _linesInMessageBox = new List<string>(MAX_LINES_IN_MESSAGE_BOX);
        private readonly IOrderCalculationService _orderCalculationService;
        private readonly SettingsViewModel _settingsViewModel;
        private readonly IRiskCalculationService _riskCalculationService;
        private readonly ITradeRecordingService _tradeRecordingService;
        private ICommand _clearCommand;
        private AsyncCommand _connectCommand;

        private string _connectButtonCaption;
        private string _errorText;
        private bool _isEnabled;
        private int _numberOfLinesInMessageBox;
        private double _riskMultiplier;
        private double _riskPerTrade;
        private ObservableCollection<MenuItemViewModel> _menuItems;
        private ObservableCollection<MenuItemViewModel> _menuOptionItems;
        private bool _isDetailsPanelVisible;        
        private bool _isBusy;
        private DefaultErrorHandler _defaultErrorHandler;

        #endregion

        #region Constructors

        public MainViewModel(
            IDispatcherHelper dispatcherHelper,
            IConnectionService connectionService,
            IOrderCalculationService orderCalculationService,
            PositionsViewModel positionsViewModel,
            SettingsViewModel settingsViewModel,
            OrdersListViewModel ordersListViewModel,
            IRiskCalculationService riskCalculationService,
            ITradeRecordingService tradeRecordingService)
            : base(dispatcherHelper)
        {
            _connectionService = connectionService;
            OrdersListViewModel = ordersListViewModel;
            _riskCalculationService = riskCalculationService;
            _tradeRecordingService = tradeRecordingService;
            PositionsViewModel = positionsViewModel;
            _settingsViewModel = settingsViewModel;
            _settingsViewModel.PropertyChanged += OnSettingsViewModelPropertyChanged;
            _orderCalculationService = orderCalculationService;

            Messenger.Default.Register<ConnectionChangedMessage>(this, HandleConnectionChangedMessage);
            Messenger.Default.Register<DetailsPanelClosedMessage>(this, HandleDetailsPanelClosed);
            Messenger.Default.Register<OrderStatusChangedMessage>(this, OrderStatusChangedMessage.Tokens.Main, HandleOrderStatusChangedMessage);

            _connectionService.ClientError += HandleClientError;
            SetConnectionStatus();
            CreateMenuItems();

            // Load settings on a different thread as it's slow and so that we can show the main window straight away
            Task.Run(() => _settingsViewModel.LoadSettingsAsync())
                .ContinueWith(t =>
                {
                    HandleLoadSettingsTaskResult(t);
                })
                .ConfigureAwait(false);
        }

        private void HandleLoadSettingsTaskResult(Task t)
        {
            RiskMultiplier = _settingsViewModel.LastRiskMultiplier;
            LoadExistingTrades();

            if (!t.IsFaulted)
            {
                return;
            }

            NotifyError(t.Exception);
        }

        private void NotifyError(Exception exception)
        {
            DispatcherHelper.InvokeOnUiThread(() =>
            {
                Log.Error("Error loading settings\n{0}", exception);
                var notification = "Error loading settings";
                Messenger.Default.Send(new NotificationMessage<NotificationType>(NotificationType.Error, notification));
            });
        }

        private void LoadExistingTrades()
        {
            var handler = new LoggingErrorHandler();
            _tradeRecordingService.LoadTradesAsync().FireAndForgetSafeAsync(handler);
        }

        #endregion

        #region Properties

        #region Commands

        public ICommand ClearCommand => _clearCommand ??= new RelayCommand(new Action(ClearLog));

        public AsyncCommand ConnectCommand => _connectCommand ??= new AsyncCommand(DispatcherHelper, ToggleConnectionAsync, () => !IsBusy, DefaultErrorHandler);

        #endregion

        private IErrorHandler DefaultErrorHandler => _defaultErrorHandler ??= new DefaultErrorHandler();

        public ObservableCollection<MenuItemViewModel> MenuItems
        {
            get => _menuItems;
            set => Set(ref _menuItems, value);
        }

        public ObservableCollection<MenuItemViewModel> MenuOptionItems
        {
            get => _menuOptionItems;
            set => Set(ref _menuOptionItems, value);
        }

        public string ConnectButtonCaption
        {
            get => _connectButtonCaption;
            set => Set(ref _connectButtonCaption, value);
        }

        public bool IsDetailsPanelVisible
        {
            get => _isDetailsPanelVisible;
            set => Set(ref _isDetailsPanelVisible, value);
        }

        public string ErrorText
        {
            get => _errorText;
            private set => Set(ref _errorText, value);
        }

        public bool IsBusy
        {
            get => _isBusy;
            set
            {
                Set(ref _isBusy, value);
                ConnectCommand.RaiseCanExecuteChanged();
            }
        }

        public bool IsEnabled
        {
            get => _isEnabled;
            set => Set(ref _isEnabled, value);
        }

        public OrdersListViewModel OrdersListViewModel { get; private set; }

        public PositionsViewModel PositionsViewModel { get; private set; }

        public double RiskMultiplier
        {
            get => _riskMultiplier;
            set
            {
                Set(ref _riskMultiplier, value);
                _settingsViewModel.LastRiskMultiplier = value;
                _riskCalculationService.SetRiskMultiplier(value);
                CalculateRiskPerTrade();
            }
        }

        public double RiskPerTrade
        {
            get => _riskPerTrade;
            set
            {
                Set(ref _riskPerTrade, value);
                _orderCalculationService.SetRiskPerTrade(value);
                foreach (var item in OrdersListViewModel.Orders)
                {
                    item.CalculateOrderDetails();
                }
            }
        }

        #endregion

        #region Methods

        public void AppIsClosing()
        {
            var errorHandler = new LoggingErrorHandler();
            if (_connectionService.IsConnected)
            {
                _connectionService.DisconnectAsync().FireAndForgetSafeAsync(errorHandler);
            }
        }

        private void AddTextToMessagePanel(string text)
        {
            HandleErrorMessage(new ErrorMessage(-1, -1, text));
        }

        private void CalculateRiskPerTrade()
        {
            RiskPerTrade = _riskCalculationService.RiskPerTrade;
        }

        private void ClearLog()
        {
            ErrorText = string.Empty;
        }

        private void CreateMenuItems()
        {
            if (IsUnitTesting)
            {
                return;
            }

            MenuItems = new ObservableCollection<MenuItemViewModel>
            {
                new HomeViewModel
                {
                    Icon = new PackIconMaterial() { Kind = PackIconMaterialKind.Home },
                    Label = "Home",
                    ToolTip = "Welcome Home"
                },
                new AboutViewModel
                {
                    Icon = new PackIconMaterial() { Kind = PackIconMaterialKind.Help },
                    Label = "About",
                    ToolTip = "About this one..."
                }
            };

            _settingsViewModel.Icon = new PackIconMaterial() { Kind = PackIconMaterialKind.Cog };
            _settingsViewModel.Label = "Settings";
            _settingsViewModel.ToolTip = "Settings for the application";

            MenuOptionItems = new ObservableCollection<MenuItemViewModel>
            {
                _settingsViewModel
            };
        }

        private string EnsureMessageHasNewline(string message)
        {
            return message.Substring(message.Length - 1) != "\n"
                ? message + "\n"
                : message;
        }

        private void HandleConnectionChangedMessage(ConnectionChangedMessage message)
        {
            SetConnectionStatus();
        }

        private void HandleDetailsPanelClosed(DetailsPanelClosedMessage message)
        {
            IsDetailsPanelVisible = false;
        }

        private void HandleErrorMessage(ErrorMessage error)
        {
            var message = $"Request {error.RequestId}, Code: {error.ErrorCode} - {error.Message}";
            ShowMessageOnPanel(message);
            Log.Error(message);
        }

        private void HandleClientError(object sender, ClientError e)
        {
            if (e.Id == 0 || e.ErrorCode == 0)
            {
                AddTextToMessagePanel("Error: " + e.ErrorMessage + "\n");
                return;
            }

            // The following are connection OK messages, as opposed to errors
            switch (e.ErrorCode)
            {
                case 2104:
                case 2106:
                case 2158:
                    return;
            }

            var error = new ErrorMessage(e.Id, e.ErrorCode, e.ErrorMessage);
            HandleErrorMessage(error);
        }

        private void OnSettingsViewModelPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName != nameof(SettingsViewModel.RiskPercentOfAccountSize))
            {
                return;
            }

            CalculateRiskPerTrade();
        }

        private void SetConnectionStatus()
        {
            var isConnected = _connectionService.IsConnected;
            ConnectButtonCaption = isConnected
                ? "Disconnect"
                : "Connect";

            IsEnabled = isConnected;
        }

        private void ShowMessageOnPanel(string message)
        {
            message = EnsureMessageHasNewline(message);

            if (_numberOfLinesInMessageBox >= MAX_LINES_IN_MESSAGE_BOX)
            {
                _linesInMessageBox.RemoveRange(0, MAX_LINES_IN_MESSAGE_BOX - REDUCED_LINES_IN_MESSAGE_BOX);
                _numberOfLinesInMessageBox = REDUCED_LINES_IN_MESSAGE_BOX;
            }

            _linesInMessageBox.Add(message);
            _numberOfLinesInMessageBox++;
            ErrorText = string.Join(string.Empty, _linesInMessageBox);
        }

        private async Task ToggleConnectionAsync()
        {
            try
            {
                IsBusy = true;
                ConnectButtonCaption = "Connecting...";

                if (_connectionService.IsConnected)
                {
                    await _connectionService.DisconnectAsync();
                }
                else
                {
                    await _connectionService.ConnectAsync();
                    SetConnectionStatus();

                    await Task.WhenAll(_riskCalculationService.RequestDataForCalculationAsync(), PositionsViewModel.GetPositionsAsync()).ConfigureAwait(false);
                    CalculateRiskPerTrade();
                }
            }
            catch (Exception ex)
            {
                Log.Error("Error in {0}:\n{1}", nameof(ToggleConnectionAsync), ex);
                DispatcherHelper.InvokeOnUiThread(() =>
                {
                    var notification = $"Error during initialisation upon connection:\n{ex.Message}";
                    Messenger.Default.Send(new NotificationMessage<NotificationType>(NotificationType.Error, notification));
                });
                ShowMessageOnPanel(ex.Message);
            }
            finally
            {
                IsBusy = false;
                SetConnectionStatus();
            }
        }

        private void HandleOrderStatusChangedMessage(OrderStatusChangedMessage message)
        {
            if (message.Message.Status != BrokerConstants.OrderStatus.Filled)
            {
                return;
            }

            // Find corresponding order
            var order = OrdersListViewModel.Orders.SingleOrDefault(o => o.Id == message.Message.OrderId);
            if (order == null)
            {
                return;
            }

            Log.Debug(message.Dump($"Order for symbol {order.Symbol.Code} was filled."));
            OrdersListViewModel.Orders.Remove(order);
            PositionsViewModel.GetPositionForSymbolAsync(order.Symbol.Code).FireAndForgetSafeAsync(new LoggingErrorHandler());

            /* Once the order has been filled, it is deleted and a request is made for the current positions, 
             * which will add it to the positions collection
             * 
             * We are more than likely on the thread that processed the API controller's event handler.  
             * Because we want to give back control to the API controller worker ASAP, we queue this processing on the queue processor
             * However the job of removing the order needs to happen on the UI thread because the ObservableCollection is not thread-safe,
             * so we use the DispatcherHelper to invoke the action
             */

            //QueueProcessor.Enqueue(() =>
            //{
            //DispatcherHelper.InvokeOnUiThread(async () =>
            //{                    
            //    //OrdersViewModel.Orders.Remove(item);
            //    await PositionsViewModel.GetPositionsAsync();
            //});
            //}); 
        }
        #endregion
    }
}
