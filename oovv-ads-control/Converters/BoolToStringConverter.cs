using System;
using System.Globalization;
using System.Windows.Data;

namespace oovv_ads_control.Converters
{
    /// <summary>
    /// 通用的"按 bool 状态二选一"转换器：ConverterParameter 写成 "真值|假值"，
    /// 绑定源是 bool 就返回对应那一半。不限定用途——图标路径、文字、颜色字符串都能用，
    /// 比如按钮图标按 IsValid 切换：
    /// Content="{Binding IsValid, Converter={StaticResource BoolToStringConverter},
    ///           ConverterParameter='/Assets/Icons/submit.png|/Assets/Icons/submit_reject.png'}"
    /// </summary>
    public sealed class BoolToStringConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is not bool flag || parameter is not string options)
                return Binding.DoNothing;

            var parts = options.Split('|');
            if (parts.Length != 2)
                return Binding.DoNothing;

            return flag ? parts[0] : parts[1];
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }
}
