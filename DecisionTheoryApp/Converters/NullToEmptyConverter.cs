using System;
using System.Globalization;
using System.Windows.Data;

namespace DecisionTheoryApp.Converters
{
    public class NullToEmptyConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null || value == DBNull.Value)
                return "";
            if (value is double d)
                return Math.Round(d, 2).ToString("G", CultureInfo.CurrentCulture);
            return value.ToString();
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string str = value as string;
            if (string.IsNullOrWhiteSpace(str))
                return DBNull.Value;
            if (double.TryParse(str.Replace(',', '.'), NumberStyles.Any, CultureInfo.InvariantCulture, out double result))
                return result;
            return DBNull.Value;
        }
    }
}