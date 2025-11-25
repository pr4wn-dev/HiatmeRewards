using System;
using System.Globalization;
using Microsoft.Maui.Controls;

namespace HiatMeApp.Converters;

public class InverseBooleanConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is bool boolean ? !boolean : false;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is bool boolean ? !boolean : false;
    }
}