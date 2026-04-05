using System;
using System.Globalization;
using System.Windows.Data;

namespace DecisionTheoryApp.Converters
{
    /// <summary>
    /// Конвертирует enum в int (для SelectedIndex) и обратно.
    /// Работает с любым enum через Convert.ToInt32/Enum.ToObject.
    /// </summary>
    public class EnumToIndexConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null) return 0;
            return System.Convert.ToInt32(value);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null) return Enum.ToObject(targetType, 0);
            try
            {
                return Enum.ToObject(targetType, System.Convert.ToInt32(value));
            }
            catch
            {
                return Enum.ToObject(targetType, 0);
            }
        }
    }
}
