using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace oovv_ads_control.Converters
{
    /// <summary>true -> Collapsed，false -> Visible，用于和内置 BooleanToVisibilityConverter 配对切换两套互斥的界面。</summary>
    public sealed class InverseBooleanToVisibilityConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
            => value is bool b && b ? Visibility.Collapsed : Visibility.Visible;

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }
}
