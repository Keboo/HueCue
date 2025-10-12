using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace HueCue;

public class EqualityToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value == null || parameter == null)
            return Visibility.Collapsed;

        // Handle double comparison with tolerance
        if (value is double doubleValue && parameter is string paramString)
        {
            if (double.TryParse(paramString, NumberStyles.Any, CultureInfo.InvariantCulture, out double paramDouble))
            {
                return Math.Abs(doubleValue - paramDouble) < 0.01 ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        return value.ToString() == parameter.ToString() ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}