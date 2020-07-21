﻿using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace MyTradingApp.Converters
{
    internal class NotBooleanToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var hide = (bool)value;
            return hide
                ? Visibility.Collapsed
                : Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
