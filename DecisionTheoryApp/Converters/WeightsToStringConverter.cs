using System;
using System.Globalization;
using System.Windows.Data;

namespace DecisionTheoryApp.Converters
{
    public class WeightsToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is double[] weights && weights.Length > 0)
            {
                return string.Join("; ", Array.ConvertAll(weights, w => $"{w * 100:F1}%"));
            }
            return "";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}