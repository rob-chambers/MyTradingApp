using GalaSoft.MvvmLight.Messaging;
using MyTradingApp.Core.ViewModels;
using MyTradingApp.Domain;
using Serilog;
using System.Threading.Tasks;

namespace MyTradingApp.Core.Services
{
    public class RiskCalculationService : IRiskCalculationService
    {
        private readonly IExchangeRateService _exchangeRateService;
        private readonly IAccountManager _accountManager;
        private readonly SettingsViewModel _settingsViewModel;
        private double _riskMultiplier = 1;
        private RiskCalculationServiceData _data;
        private Task<double> _exchangeRateTask;
        private Task<AccountSummary> _accountSummaryTask;

        public RiskCalculationService(
            IExchangeRateService exchangeRateService,
            IAccountManager accountManager,
            SettingsViewModel settingsViewModel)
        {
            _exchangeRateService = exchangeRateService;
            _accountManager = accountManager;
            _settingsViewModel = settingsViewModel;
        }

        public double RiskPerTrade
        {
            get
            {
                var maxCapital = _data.AccountSummary.NetLiquidation * _settingsViewModel.RiskPercentOfAccountSize / 100;
                return maxCapital * _data.ExchangeRate * _riskMultiplier;
            }
        }

        public void SetRiskMultiplier(double riskMultipier)
        {
            _riskMultiplier = riskMultipier;
        }

        public async Task RequestDataForCalculationAsync()
        {
            Log.Debug("Start of {0}", nameof(RequestDataForCalculationAsync));

            InitTasks();
            await ExecuteTasks().ConfigureAwait(false);
            _data = await GetResultsAsync().ConfigureAwait(false);

            Messenger.Default.Send(new EventMessages.AccountSummaryMessage(_data.AccountSummary));            
        }

        private void InitTasks()
        {
            _exchangeRateTask = _exchangeRateService.GetExchangeRateAsync();
            _accountSummaryTask = _accountManager.RequestAccountSummaryAsync();
        }

        private Task ExecuteTasks()
        {
            return Task.WhenAll(_exchangeRateTask, _accountSummaryTask);
        }

        private async Task<RiskCalculationServiceData> GetResultsAsync()
        {
            var accountSummary = await _accountSummaryTask.ConfigureAwait(false);
            var exchangeRate = await _exchangeRateTask.ConfigureAwait(false);

            return new RiskCalculationServiceData(exchangeRate, accountSummary);
        }
    }
}
