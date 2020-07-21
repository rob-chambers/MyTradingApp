using MyTradingApp.Core.Repositories;
using MyTradingApp.Domain;
using System.Collections.Generic;

namespace MyTradingApp.ViewModels
{
    public class SettingsViewModel : MenuItemViewModel
    {        
        private bool _isLoading;
        private double _riskPercentOfAccountSize;
        private double _lastRiskMultiplier;
        private readonly ISettingsRepository _settingsRepository;
        private Dictionary<string, Setting> _settings;

        public SettingsViewModel(ISettingsRepository settingsRepository)
        {
            _settingsRepository = settingsRepository;
            LoadSettings();            
        }

        /*
         *         public double RiskPercentOfAccountSize { get; set; } = 0.5;

        public double LastRiskMultiplier { get; set; } = 1.0;

         */

        private void LoadSettings()
        {
            _isLoading = true;
            try
            {
                _settings = new Dictionary<string, Setting>();
                foreach (var item in _settingsRepository.GetAll())
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

        public double RiskPercentOfAccountSize
        {
            get => _riskPercentOfAccountSize;
            set
            {
                Set(ref _riskPercentOfAccountSize, value);
                if (!_isLoading)
                {
                    SetValue("RiskPercentOfAccountSize", value.ToString());
                    //_settings.RiskPercentOfAccountSize = value;
                }
            }
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

        public double LastRiskMultiplier
        {
            get => _lastRiskMultiplier;
            set
            {
                Set(ref _lastRiskMultiplier, value);
                if (!_isLoading)
                {
                    SetValue("LastRiskMultiplier", value.ToString());
                    //_settings.LastRiskMultiplier = value;
                }
            }
        }

        public void Save()
        {
            _settingsRepository.Save(_settings.Values);
        }
    }
}