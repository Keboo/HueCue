using System.Globalization;
using System.Windows.Data;

namespace HueCue;

/// <summary>
/// Converts a dimension (width or height) to a rule of thirds position.
/// ConverterParameter: 1 for first third, 2 for second third
/// </summary>
public class ThirdsConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is double dimension && parameter is string paramStr && int.TryParse(paramStr, out int third))
        {
            return dimension * third / 3.0;
        }
        return 0.0;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
