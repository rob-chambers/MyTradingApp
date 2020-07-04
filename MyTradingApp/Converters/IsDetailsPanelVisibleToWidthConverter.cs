using System;
using System.Globalization;
using System.Windows.Data;

namespace MyTradingApp.Converters
{
    internal class IsDetailsPanelVisibleToWidthConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var show = (bool)value;
            return show
                ? 230
                : 0;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
