using System.Globalization;
using System.Windows.Data;

namespace HueCue;

public class GuideOverlayToBooleanConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is GuideOverlay overlay && parameter is GuideOverlay param)
        {
            return overlay == param;
        }
        return false;
    }
    
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool isChecked && parameter is GuideOverlay param)
        {
            return isChecked ? param : GuideOverlay.None;
        }
        return Binding.DoNothing;
    }
}