using MyTradingApp.Core.Repositories;
using MyTradingApp.Domain;
using Serilog;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MyTradingApp.ViewModels
{
    public class SettingsViewModel : MenuItemViewModel
    {
        private readonly ISettingsRepository _settingsRepository;
        private bool _isLoading;
        private double _riskPercentOfAccountSize;
        private double _lastRiskMultiplier;        
        private Dictionary<string, Setting> _settings;

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
                    SetValue("LastRiskMultiplier", value.ToString());
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
                    SetValue("RiskPercentOfAccountSize", value.ToString());
                }
            }
        }

        public async Task LoadSettingsAsync()
        {
            Log.Information("Loading settings");
            _isLoading = true;
            try
            {
                _settings = new Dictionary<string, Setting>();

                var items = await _settingsRepository.GetAllAsync().ConfigureAwait(false);
                foreach (var item in items)
                {
                    _settings.Add(item.Key, new Setting
                    {
                        Key = item.Key,
                        Value = item.Value
                    });
                }

                var value = GetSetting("RiskPercentOfAccountSize") ?? "0.5";
                RiskPercentOfAccountSize = StringToDouble(value);
                value = GetSetting("LastRiskMultiplier") ?? "1";
                LastRiskMultiplier = StringToDouble(value);
            }
            finally
            {
                _isLoading = false;
            }
        }

        public async Task SaveAsync()
        {
            Log.Information("Saving settings");
            await _settingsRepository.SaveAsync(_settings.Values).ConfigureAwait(false);
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
            }
            else
            {
                setting = new Setting
                {
                    Key = key,
                    Value = value
                };
                _settings.Add(setting.Key, setting);
            }
        }
    }
}