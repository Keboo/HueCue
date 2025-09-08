using System.Globalization;
using System.Windows.Data;

namespace HueCue;

public class BoolToPlayPauseTextConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool isPlaying)
        {
            return isPlaying ? "_Pause" : "_Play";
        }
        return "_Play";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
