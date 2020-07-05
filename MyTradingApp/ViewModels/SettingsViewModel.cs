namespace MyTradingApp.ViewModels
{
    internal class SettingsViewModel : MenuItemViewModel
    {
        private double _riskPercentOfAccountSize;
        private bool _isLoading;
        private double _lastRiskMultiplier;

        public SettingsViewModel()
        {
            LoadSettings();
        }

        private void LoadSettings()
        {
            _isLoading = true;
            try
            {
                RiskPercentOfAccountSize = Properties.Settings.Default.RiskPercentOfAccountSize;
                LastRiskMultiplier = Properties.Settings.Default.LastRiskMultiplier;
            }
            finally
            {
                _isLoading = false;
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
                    Properties.Settings.Default.RiskPercentOfAccountSize = value;
                }                
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
                    Properties.Settings.Default.LastRiskMultiplier = value;
                }
            }
        }

        public void Save()
        {
            Properties.Settings.Default.Save();
        }
    }
}