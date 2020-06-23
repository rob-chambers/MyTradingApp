using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Messaging;
using MyTradingApp.EventMessages;
using MyTradingApp.Messages;
using MyTradingApp.Models;
using MyTradingApp.Services;
using System;
using System.Collections.Generic;
using System.Windows.Input;

namespace MyTradingApp.ViewModels
{
    internal class MainViewModel : ViewModelBase
    {
        private const int MAX_LINES_IN_MESSAGE_BOX = 200;
        private const int REDUCED_LINES_IN_MESSAGE_BOX = 100;
        private readonly List<string> _linesInMessageBox = new List<string>(MAX_LINES_IN_MESSAGE_BOX);
        private readonly IBClient _iBClient;
        private readonly IConnectionService _connectionService;
        private readonly IOrderManager _orderManager;
        private readonly IAccountManager _accountManager;
        private readonly StatusBarViewModel _statusBarViewModel;
        private readonly IHistoricalDataManager _historicalDataManager;
        private readonly IExchangeRateService _exchangeRateService;
        private readonly IOrderCalculationService _orderCalculationService;
        private ICommand _connectCommand;
        private ICommand _accountSummaryCommand;
        private ICommand _clearCommand;
        private string _connectButtonCaption;
        private int _numberOfLinesInMessageBox;
        private int _orderId;
        private int _parentOrderId;
        private string _errorText;
        private bool _isEnabled;
        private double _riskMultiplier;
        private double _exchangeRate;
        private double _netLiquidation;

        public MainViewModel(
            IBClient iBClient,
            IConnectionService connectionService,
            IOrderManager orderManager,
            IAccountManager accountManager,
            OrdersViewModel ordersViewModel,
            StatusBarViewModel statusBarViewModel,
            IHistoricalDataManager historicalDataManager,
            IExchangeRateService exchangeRateService,
            IOrderCalculationService orderCalculationService)
        {
            _iBClient = iBClient;
            _connectionService = connectionService;
            _orderManager = orderManager;
            _accountManager = accountManager;
            OrdersViewModel = ordersViewModel;
            _statusBarViewModel = statusBarViewModel;
            _historicalDataManager = historicalDataManager;
            _exchangeRateService = exchangeRateService;
            _orderCalculationService = orderCalculationService;
            _iBClient.HistoricalData += _historicalDataManager.HandleMessage;
            _iBClient.HistoricalDataUpdate += _historicalDataManager.HandleMessage;
            _iBClient.HistoricalDataEnd += _historicalDataManager.HandleMessage;
            _iBClient.OrderStatus += _orderManager.HandleOrderStatus;
            _iBClient.AccountSummary += accountManager.HandleAccountSummary;
            _iBClient.AccountSummaryEnd += UpdateUI;

            Messenger.Default.Register<ExchangeRateMessage>(this, OnExchangeRateMessage);
            Messenger.Default.Register<AccountSummaryCompletedMessage>(this, HandleAccountSummaryMessage);

            _connectionService.ClientError += OnClientError;
            SetConnectionStatus();
            RiskMultiplier = 1.0;
        }

        private void HandleAccountSummaryMessage(AccountSummaryCompletedMessage message)
        {
            _netLiquidation = message.NetLiquidation;
            RaisePropertyChanged(nameof(RiskPerTrade));
        }

        private void OnExchangeRateMessage(ExchangeRateMessage message)
        {
            _exchangeRate = message.Price;
            RaisePropertyChanged(nameof(RiskPerTrade));
        }

        public bool IsEnabled
        {
            get => _isEnabled;
            set => Set(ref _isEnabled, value);
        }

        public double RiskMultiplier
        {
            get => _riskMultiplier;
            set
            {
                Set(ref _riskMultiplier, value);
                _orderCalculationService.SetRiskPerTrade(value);
                RaisePropertyChanged(nameof(RiskPerTrade));
            }
        }

        public double RiskPerTrade 
        {
            get => _netLiquidation * 0.01 * _exchangeRate * RiskMultiplier;
            set
            {
                _orderCalculationService.SetRiskPerTrade(value);
            }
        }

        private void OnClientError(object sender, ClientError e)
        {
            if (e.Exception != null)
            {
                AddTextToBox("Error: " + e.Exception);
                return;
            }

            if (e.Id == 0 || e.ErrorCode == 0)
            {
                AddTextToBox("Error: " + e.ErrorMessage + "\n");
                return;
            }

            var error = new ErrorMessage(e.Id, e.ErrorCode, e.ErrorMessage);
            HandleErrorMessage(error);
        }

        private void AddTextToBox(string text)
        {
            HandleErrorMessage(new ErrorMessage(-1, -1, text));
        }

        private void UpdateUI(AccountSummaryEndMessage message)
        {
            _accountManager.HandleAccountSummaryEnd();
        }

        public string ErrorText
        {
            get => _errorText;
            private set => Set(ref _errorText, value);
        }

        public ICommand ConnectCommand => _connectCommand ?? (_connectCommand = new RelayCommand(new Action(ToggleConnection)));

        public ICommand AccountSummaryCommand => _accountSummaryCommand ?? (_accountSummaryCommand = new RelayCommand(new Action(GetAccountSummary)));

        public ICommand ClearCommand => _clearCommand ?? (_clearCommand = new RelayCommand(new Action(ClearLog)));

        private void ClearLog()
        {
            ErrorText = string.Empty;
        }

        public OrdersViewModel OrdersViewModel { get; private set; }

        public string ConnectButtonCaption
        {
            get => _connectButtonCaption;
            set => Set(ref _connectButtonCaption, value);
        }

        private void ToggleConnection()
        {
            try
            {
                if (_connectionService.IsConnected)
                {
                    _connectionService.Disconnect();
                }
                else
                {
                    _connectionService.Connect();
                    if (_connectionService.IsConnected)
                    {
                        // Fire off account summary command
                        AccountSummaryCommand.Execute(null);

                        // Send a request to get the exchange rate
                        _exchangeRateService.RequestExchangeRate();
                    }
                }

                SetConnectionStatus();
            }
            catch (Exception ex)
            {
                ShowMessageOnPanel(ex.Message);
            }
        }

        private void GetAccountSummary()
        {
            _accountManager.RequestAccountSummary();
        }

        public void AppIsClosing()
        {
            if (_connectionService.IsConnected)
            {
                _connectionService.Disconnect();
            }
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

        private void HandleErrorMessage(ErrorMessage message)
        {
            ShowMessageOnPanel("Request " + message.RequestId + ", Code: " + message.ErrorCode + " - " + message.Message);
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

        private string EnsureMessageHasNewline(string message)
        {
            return message.Substring(message.Length - 1) != "\n"
                ? message + "\n"
                : message;
        }
    }
}
