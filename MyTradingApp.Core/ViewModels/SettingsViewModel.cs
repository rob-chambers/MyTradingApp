using MyTradingApp.Core.Repositories;
using MyTradingApp.Core.Utils;
using MyTradingApp.Domain;
using Serilog;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MyTradingApp.Core.ViewModels
{
    public class SettingsViewModel : MenuItemViewModel
    {
        private readonly ISettingsRepository _settingsRepository;
        private readonly Dictionary<string, Setting> _settings = new Dictionary<string, Setting>();
        private bool _isLoading;
        private double _riskPercentOfAccountSize;
        private double _lastRiskMultiplier;

        public static class SettingsKeys
        {
            public const string RiskMultiplier = "LastRiskMultiplier";
            public const string RiskPercentOfAccountSize = "RiskPercentOfAccountSize";
        }

        public SettingsViewModel(ISettingsRepository settingsRepository)
        {
            _settingsRepository = settingsRepository;
        }

        public double LastRiskMultiplier
        {
            get => _lastRiskMultiplier;
            set
            {
                Set(ref _lastRiskMultiplier, value);
                if (!_isLoading)
                {
                    SetValue(SettingsKeys.RiskMultiplier, value.ToString());
                }
            }
        }

        public double RiskPercentOfAccountSize
        {
            get => _riskPercentOfAccountSize;
            set
            {
                Set(ref _riskPercentOfAccountSize, value);
                if (!_isLoading)
                {
                    SetValue(SettingsKeys.RiskPercentOfAccountSize, value.ToString());
                }
            }
        }

        public async Task LoadSettingsAsync()
        {
            Log.Information("Loading settings");
            _isLoading = true;
            try
            {
                var items = await _settingsRepository.GetAllAsync().ConfigureAwait(false);
                foreach (var item in items)
                {
                    _settings.Add(item.Key, new Setting
                    {
                        Key = item.Key,
                        Value = item.Value
                    });
                }

                var value = GetSetting(SettingsKeys.RiskPercentOfAccountSize) ?? "0.5";
                RiskPercentOfAccountSize = StringToDouble(value);
                value = GetSetting(SettingsKeys.RiskMultiplier) ?? "1";
                LastRiskMultiplier = StringToDouble(value);
            }
            finally
            {
                _isLoading = false;
            }
        }

        private static double StringToDouble(string value)
        {
            return double.TryParse(value, out var doubleValue)
                ? doubleValue
                : 0;
        }

        private string GetSetting(string key)
        {
            if (_settings.ContainsKey(key))
            {
                return _settings[key].Value;
            }

            return null;
        }

        private void SetValue(string key, string value)
        {
            Setting setting;
            if (_settings.ContainsKey(key))
            {
                setting = _settings[key];
                setting.Value = value;
                _settingsRepository.Update(setting);
            }
            else
            {
                setting = new Setting
                {
                    Key = key,
                    Value = value
                };
                _settings.Add(setting.Key, setting);
                _settingsRepository.Add(setting);
            }

            // Save settings in the background
            Task.Run(() => _settingsRepository.SaveAsync().FireAndForgetSafeAsync(new LoggingErrorHandler()));
        }
    }
}