using System;
using System.Globalization;
using System.Windows.Data;

namespace DecisionTheoryApp.Converters
{
    /// <summary>
    /// Конвертер для инвертирования булевых значений
    /// </summary>
    /// 
    public class InverseBooleanConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
                return !boolValue;

            return true;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
                return !boolValue;

            return false;
        }
    }
}