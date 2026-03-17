using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace DecisionTheoryApp.Converters
{
    /// <summary>
    /// Конвертер булевых значений в Visibility
    /// </summary>
    public class BooleanToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                // Если параметр "Inverse" - инвертируем логику
                if (parameter is string param && param == "Inverse")
                {
                    return boolValue ? Visibility.Collapsed : Visibility.Visible;
                }

                return boolValue ? Visibility.Visible : Visibility.Collapsed;
            }

            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Visibility visibility)
            {
                bool result = visibility == Visibility.Visible;

                // Если параметр "Inverse" - инвертируем результат
                if (parameter is string param && param == "Inverse")
                {
                    return !result;
                }

                return result;
            }

            return false;
        }
    }
}