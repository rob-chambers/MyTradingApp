﻿using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace MyTradingApp.Converters
{
    internal class ColourConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            Brush color = null;
            var num = (double)value;
            color = (num >= 0) 
                ? Brushes.Green 
                : Brushes.Red;

            return color;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}