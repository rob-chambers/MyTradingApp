using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using IBApi;
using MyTradingApp.Messages;
using MyTradingApp.Models;
using MyTradingApp.Services;
using System;
using System.Collections.Generic;
using System.Globalization;
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
        private ICommand _connectCommand;
        private ICommand _sendOrderCommand;
        private ICommand _accountSummaryCommand;
        private string _connectButtonCaption;
        private int _numberOfLinesInMessageBox;
        private int _orderId;
        private int _parentOrderId;
        private string _errorText;

        public MainViewModel(
            IBClient iBClient,
            IConnectionService connectionService,
            IOrderManager orderManager,
            IAccountManager accountManager,
            OrdersViewModel ordersViewModel,
            StatusBarViewModel statusBarViewModel,
            IHistoricalDataManager historicalDataManager)
        {
            _iBClient = iBClient;
            _connectionService = connectionService;
            _orderManager = orderManager;
            _accountManager = accountManager;
            _accountManager.AccountSummary += OnAccountManagerAccountSummary;
            OrdersViewModel = ordersViewModel;
            _statusBarViewModel = statusBarViewModel;
            _historicalDataManager = historicalDataManager;
            _iBClient.HistoricalData += _historicalDataManager.HandleMessage;
            _iBClient.HistoricalDataUpdate += _historicalDataManager.HandleMessage;
            _iBClient.HistoricalDataEnd += _historicalDataManager.HandleMessage;
            _iBClient.OrderStatus += _orderManager.HandleOrderStatus;
            _iBClient.AccountSummary += accountManager.HandleAccountSummary;
            _iBClient.AccountSummaryEnd += UpdateUI;

            _connectionService.ConnectionStatusChanged += (sender, args) => UpdateUI(new ConnectionStatusMessage(args.IsConnected));
            _connectionService.ClientError += OnClientError;
            _connectionService.ManagedAccounts += OnManagedAccounts;
            SetConnectionStatus();
        }

        private void OnAccountManagerAccountSummary(object sender, AccountSummaryEventArgs e)
        {
            _statusBarViewModel.AvailableFunds = e.AvailableFunds.ToString("C", CultureInfo.GetCultureInfo("en-US"));
            _statusBarViewModel.BuyingPower = e.BuyingPower.ToString("C", CultureInfo.GetCultureInfo("en-US"));
        }

        private void OnManagedAccounts(object sender, ManagedAccountsEventArgs args)
        {
            _orderManager.ManagedAccounts = args.Message.ManagedAccounts;
            //accountManager.ManagedAccounts = message.ManagedAccounts;
            //exerciseAccount.Items.AddRange(message.ManagedAccounts.ToArray());
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

        private void UpdateUI(ConnectionStatusMessage statusMessage)
        {
            if (statusMessage.IsConnected)
            {
                _statusBarViewModel.ConnectionStatusText = "Connected to TWS";
            }
            else
            {
                _statusBarViewModel.ConnectionStatusText = "Disconnected...";
            }
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

        public ICommand ConnectCommand => _connectCommand ?? (_connectCommand = new RelayCommand(new Action(Connect)));

        public ICommand SendOrderCommand => _sendOrderCommand ?? (_sendOrderCommand = new RelayCommand(new Action(SendOrder)));

        public ICommand AccountSummaryCommand => _accountSummaryCommand ?? (_accountSummaryCommand = new RelayCommand(new Action(GetAccountSummary)));

        public OrdersViewModel OrdersViewModel { get; private set; }

        private void SendOrder()
        {
            var contract = GetOrderContract();
            var order = GetOrder();
            _orderManager.PlaceOrder(contract, order);
            if (_orderId != 0)
            {
                _orderId = 0;
            }
        }

        private Contract GetOrderContract()
        {
            var contract = new Contract();

            /* To be entered in UI */
            var symbol = "JKS";
            var localSymbol = string.Empty;
            var primaryExchange = "NYSE";
            /* To be entered in UI */

            contract.Symbol = symbol;
            contract.SecType = "STK"; // Stock
            contract.Currency = "USD"; // US Dollars
            contract.Exchange = "IDEALPRO"; // Exchange
            contract.LastTradeDateOrContractMonth = string.Empty;
            contract.LocalSymbol = localSymbol;
            contract.PrimaryExch = primaryExchange;
            return contract;
        }

        private Order GetOrder()
        {
            var order = new Order();
            if (_orderId != 0)
            {
                order.OrderId = _orderId;
            }

            if (_parentOrderId != 0)
            {
                order.ParentId = _parentOrderId;
            }

            /* actions:
             * "BUY",
            "SELL",
            "SSHORT"});
            */

            order.Action = "BUY";

            /* Order types
            "MKT",
            "LMT",
            "STP",
            "STP LMT",
            "REL",
            "TRAIL",
            */

            order.OrderType = "STP";

            var stopPrice = 16.40D;
            order.AuxPrice = stopPrice;
            order.TotalQuantity = 111;
            order.Account = "DU1070034";
            order.ModelCode = string.Empty;

            /* Time in force values              
            "DAY",
            "GTC",
            "OPG",
            "IOC",
            "GTD",
            "GTT",
            "AUC",
            "FOK",
            "GTX",
            "DTC" */

            order.Tif = "DAY";
            //FillExtendedOrderAttributes(order);
            //FillAdvisorAttributes(order);
            //FillVolatilityAttributes(order);
            //FillScaleAttributes(order);
            //FillAlgoAttributes(order);
            //FillPegToBench(order);
            //FillAdjustedStops(order);
            //FillConditions(order);

            return order;
        }

        public string ConnectButtonCaption
        {
            get => _connectButtonCaption;
            set => Set(ref _connectButtonCaption, value);
        }

        public void Connect()
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

                    // Fire off account summary command
                    AccountSummaryCommand.Execute(null);
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
            ConnectButtonCaption = _connectionService.IsConnected
                ? "Disconnect"
                : "Connect";
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
            ErrorText = string.Join(Environment.NewLine, _linesInMessageBox);
        }

        private string EnsureMessageHasNewline(string message)
        {
            return message.Substring(message.Length - 1) != "\n"
                ? message + "\n"
                : message;
        }
    }
}
