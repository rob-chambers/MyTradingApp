namespace MyTradingApp.ViewModels
{
    internal class SettingsViewModel : MenuItemViewModel
    {
        private double _riskPercentOfAccountSize;
        private bool _isLoading;

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

        public void Save()
        {
            Properties.Settings.Default.Save();
        }
    }
}