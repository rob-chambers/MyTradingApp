using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Messaging;
using MahApps.Metro.IconPacks;
using MyTradingApp.Core.Utils;
using MyTradingApp.Core.ViewModels;
using MyTradingApp.Domain;
using MyTradingApp.EventMessages;
using MyTradingApp.Messages;
using MyTradingApp.Services;
using MyTradingApp.Utils;
using Serilog;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows.Input;

namespace MyTradingApp.ViewModels
{
    public class MainViewModel : DispatcherViewModel
    {
        #region Fields
        public static bool IsUnitTesting = false;

        private const int MAX_LINES_IN_MESSAGE_BOX = 200;

        private const int REDUCED_LINES_IN_MESSAGE_BOX = 100;

        private readonly IAccountManager _accountManager;
        private readonly IConnectionService _connectionService;
        private readonly IExchangeRateService _exchangeRateService;
        private readonly IBClient _iBClient;
        private readonly List<string> _linesInMessageBox = new List<string>(MAX_LINES_IN_MESSAGE_BOX);
        private readonly IOrderCalculationService _orderCalculationService;
        private readonly IOrderManager _orderManager;
        private readonly StatusBarViewModel _statusBarViewModel;
        
        private ICommand _clearCommand;        
        private ICommand _connectCommand;
        
        private string _connectButtonCaption;
        private string _errorText;
        private double _exchangeRate;
        private bool _isEnabled;
        private double _netLiquidation;
        private int _numberOfLinesInMessageBox;
        private double _riskMultiplier;
        private double _riskPerTrade;
        private ObservableCollection<MenuItemViewModel> _menuItems;
        private ObservableCollection<MenuItemViewModel> _menuOptionItems;
        private bool _isDetailsPanelVisible;
        private SettingsViewModel _settingsViewModel;

        #endregion

        #region Constructors

        public MainViewModel(
            IDispatcherHelper dispatcherHelper,
            IBClient iBClient,
            IConnectionService connectionService,
            IOrderManager orderManager,
            IAccountManager accountManager,
            OrdersViewModel ordersViewModel,
            StatusBarViewModel statusBarViewModel,
            IExchangeRateService exchangeRateService,
            IOrderCalculationService orderCalculationService,
            PositionsViewModel positionsViewModel,
            DetailsViewModel detailsViewModel,
            SettingsViewModel settingsViewModel)
            : base(dispatcherHelper)
        {           
            _iBClient = iBClient;
            _connectionService = connectionService;
            _orderManager = orderManager;
            _accountManager = accountManager;
            OrdersViewModel = ordersViewModel;
            OrdersViewModel.Orders.CollectionChanged += OnOrdersCollectionChanged;
            OrdersViewModel.PropertyChanged += OnOrdersViewModelPropertyChanged;
            PositionsViewModel = positionsViewModel;
            DetailsViewModel = detailsViewModel;
            _settingsViewModel = settingsViewModel;
            _settingsViewModel.PropertyChanged += OnSettingsViewModelPropertyChanged;
            _statusBarViewModel = statusBarViewModel;
            _exchangeRateService = exchangeRateService;
            _orderCalculationService = orderCalculationService;

            Messenger.Default.Register<ConnectionChangedMessage>(this, HandleConnectionChangedMessage);
            Messenger.Default.Register<DetailsPanelClosedMessage>(this, HandleDetailsPanelClosed);

            _connectionService.ClientError += HandleClientError;
            SetConnectionStatus();

            RiskMultiplier = _settingsViewModel.LastRiskMultiplier;

            CreateMenuItems();
        }

        #endregion

        #region Properties

        #region Commands

        public ICommand ClearCommand => _clearCommand ?? (_clearCommand = new RelayCommand(new Action(ClearLog)));

        public ICommand ConnectCommand => _connectCommand ?? (_connectCommand = new AsyncCommand(DispatcherHelper, ToggleConnectionAsync));

#endregion

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

        public bool IsEnabled
        {
            get => _isEnabled;
            set => Set(ref _isEnabled, value);
        }

        public OrdersViewModel OrdersViewModel { get; private set; }

        public PositionsViewModel PositionsViewModel { get; private set; }

        public DetailsViewModel DetailsViewModel { get; }

        public double RiskMultiplier
        {
            get => _riskMultiplier;
            set
            {
                Set(ref _riskMultiplier, value);
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
                OrdersViewModel.RecalculateRiskForAllOrders();
            }
        }

#endregion

        #region Methods

        public async void AppIsClosing()
        {
            if (_connectionService.IsConnected)
            {
                _connectionService.Disconnect();
                await _connectionService.DisconnectAsync();                
            }

            _settingsViewModel.LastRiskMultiplier = RiskMultiplier;
            _settingsViewModel?.Save();
        }

        private void AddTextToMessagePanel(string text)
        {
            HandleErrorMessage(new ErrorMessage(-1, -1, text));
        }

        private void CalculateRiskPerTrade()
        {
            RiskPerTrade = _netLiquidation * _settingsViewModel.RiskPercentOfAccountSize / 100 * _exchangeRate * RiskMultiplier;
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

        private void HandleErrorMessage(ErrorMessage message)
        {
            ShowMessageOnPanel("Request " + message.RequestId + ", Code: " + message.ErrorCode + " - " + message.Message);
            Log.Error(message.ErrorCode + " - " + message.Message);
        }

        private void HandleClientError(object sender, ClientError e)
        {
            if (e.Exception != null)
            {
                AddTextToMessagePanel("Error: " + e.Exception);
                return;
            }

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

        private void OnOrdersCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.NewItems != null)
            {
                foreach (OrderItem item in e.NewItems)
                {
                    item.PropertyChanged += OnItemPropertyChanged;
                }
            }

            if (e.OldItems != null)
            {
                foreach (OrderItem item in e.OldItems)
                {
                    item.PropertyChanged -= OnItemPropertyChanged;
                }
            }
        }

        private void OnItemPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName != nameof(OrderItem.Status))
            {
                return;
            }

            var item = (OrderItem)sender;
            var status = item.Status;
            if (status != OrderStatus.Filled)
            {
                return;
            }            

            // Once the order has been filled, it is deleted and a request is made for the current positions, which will add it to the positions collection
            OrdersViewModel.Orders.Remove(item);

            PositionsViewModel.GetPositions();
        }

        private void OnOrdersViewModelPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName != nameof(OrdersViewModel.SelectedOrder))
            {
                return;
            }

            DetailsViewModel.Selection = OrdersViewModel.SelectedOrder;
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

            _statusBarViewModel.ConnectionStatusText = isConnected
                ? "Connected to TWS"
                : "Disconnected...";

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
                if (_connectionService.IsConnected)
                {
                    _connectionService.Disconnect();
                    await _connectionService.DisconnectAsync();
                }
                else
                {
                    _connectionService.Connect();
                    await _connectionService.ConnectAsync();
                    await InitOnceConnectedAsync();
                }
            }
            catch (Exception ex)
            {
                ShowMessageOnPanel(ex.Message);
            }
        }

        private async Task InitOnceConnectedAsync()
        {
            var rate = await _exchangeRateService.GetExchangeRateAsync();
            _exchangeRate = rate;
            PositionsViewModel.GetPositions();

            var summary = await _accountManager.RequestAccountSummaryAsync();
            Messenger.Default.Send(summary);
            _netLiquidation = summary.NetLiquidation;
            CalculateRiskPerTrade();
        }
        #endregion
    }
}
