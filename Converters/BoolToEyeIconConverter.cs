using System;
using System.Globalization;
using Microsoft.Maui.Controls;

namespace HiatMeApp.Converters;

public class BoolToEyeIconConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        // Return null if files don't exist to avoid build errors
        // The converter is registered but not currently used in XAML
        // If needed in the future, ensure eye.png and eye_off.png exist in Resources/Images
        return null;
        
        // Original implementation (commented out until PNG files are added):
        // if (value is bool isVisible && isVisible)
        //     return "eye_off.png"; // Password visible, show "off" icon
        // return "eye.png"; // Password hidden, show "on" icon
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}