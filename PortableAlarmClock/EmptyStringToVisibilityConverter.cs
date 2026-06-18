// v1.00 20260617 23:59
using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace PortableAlarmClock
{
    public class EmptyStringToVisibilityConverter : IValueConverter
    {
        public static readonly EmptyStringToVisibilityConverter Instance = new EmptyStringToVisibilityConverter();

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string str && !string.IsNullOrEmpty(str))
            {
                return Visibility.Visible;
            }
            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
// v1.00 20260617 23:59
