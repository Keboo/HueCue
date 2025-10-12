using System.Globalization;
using System.Windows.Data;

namespace HueCue;

public class OpacityToBooleanConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return (value, parameter) switch
        {
            (double doubleValue, string paramString) when double.TryParse(paramString, NumberStyles.Any, CultureInfo.InvariantCulture, out double paramDouble)
                => Math.Abs(doubleValue - paramDouble) < 0.01,
            _ => false
        };
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool isChecked && isChecked && parameter is string paramString
            && double.TryParse(paramString, NumberStyles.Any, CultureInfo.InvariantCulture, out double paramDouble))
        {
            return paramDouble;
        }
        return Binding.DoNothing;
    }
}