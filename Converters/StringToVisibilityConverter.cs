using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace DocuFiller.Converters
{
    /// <summary>
    /// 字符串到可见性转换器
    /// 当字符串为空或null时返回Visible，否则返回Collapsed
    /// </summary>
    public class StringToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string str)
            {
                // 当字符串为空或null时显示提示文本，否则隐藏
                return string.IsNullOrEmpty(str) ? Visibility.Visible : Visibility.Collapsed;
            }
            
            return Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}