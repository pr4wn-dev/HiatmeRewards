using Microsoft.Maui.Controls;

namespace HiatmeApp.Converters;

public class BoolToEyeIconConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
    {
        if (value is bool isVisible && isVisible)
            return "eye_off.png"; // Password visible, show "off" icon
        return "eye.png"; // Password hidden, show "on" icon
    }

    public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}