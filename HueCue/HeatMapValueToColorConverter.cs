using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace HueCue;

/// <summary>
/// Converts a heat map value (0.0 to 1.0) to a color brush.
/// 0.0 = Red (bad composition), 1.0 = Green (good composition)
/// </summary>
public class HeatMapValueToColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is double heatValue)
        {
            // Clamp value between 0.0 and 1.0
            heatValue = Math.Max(0.0, Math.Min(1.0, heatValue));
            
            // Linear interpolation from red (0.0) to green (1.0)
            byte red = (byte)(255 * (1 - heatValue));
            byte green = (byte)(255 * heatValue);
            byte blue = 0;
            
            return new SolidColorBrush(Color.FromRgb(red, green, blue));
        }
        
        // Default to transparent if value is not a double
        return new SolidColorBrush(Colors.Transparent);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
