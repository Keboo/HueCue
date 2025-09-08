using System.Globalization;
using System.Windows.Data;

namespace HueCue;

public class HistogramOverlayToBooleanConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is HistogramOverlay overlay && parameter is HistogramOverlay param)
        {
            return overlay == param;
        }
        return false;
    }
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool isChecked && parameter is HistogramOverlay param)
        {
            return isChecked ? param : HistogramOverlay.None;
        }
        return Binding.DoNothing;
    }
}