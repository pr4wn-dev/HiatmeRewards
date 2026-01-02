using System;
using System.Globalization;
using Microsoft.Maui.Controls;

namespace HiatMeApp.Converters;

public class NotNullConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        bool isNotNull = value != null;
        
        // Handle "Inverse" parameter
        if (parameter is string param && param == "Inverse")
        {
            return !isNotNull;
        }
        
        return isNotNull;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}