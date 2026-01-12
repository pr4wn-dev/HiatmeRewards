using System;
using System.Globalization;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;

namespace HiatMeApp.Converters
{
    /// <summary>
    /// Converts menu item text to its corresponding accent color
    /// </summary>
    public class MenuItemColorConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is string text)
            {
                return text switch
                {
                    "Home" => Color.FromArgb("#3b82f6"),           // Blue
                    "Profile" => Color.FromArgb("#8b5cf6"),        // Purple
                    "Request Day Off" => Color.FromArgb("#8b5cf6"), // Purple
                    "Vehicle" => Color.FromArgb("#14b8a6"),        // Teal
                    "Vehicle Issues" => Color.FromArgb("#f97316"), // Orange
                    "Finish Day" => Color.FromArgb("#22c55e"),     // Green
                    "Login" => Color.FromArgb("#3b82f6"),          // Blue
                    "Logout" => Color.FromArgb("#ef4444"),         // Red
                    "Register" => Color.FromArgb("#f59e0b"),       // Gold
                    _ => Color.FromArgb("#3b82f6")                 // Default blue
                };
            }
            return Color.FromArgb("#3b82f6");
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Converts menu item text to its corresponding Font Awesome icon glyph
    /// </summary>
    public class MenuItemIconConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is string text)
            {
                return text switch
                {
                    "Home" => "\uf015",           // fa-home
                    "Profile" => "\uf007",        // fa-user
                    "Request Day Off" => "\uf073", // fa-calendar
                    "Vehicle" => "\uf1b9",        // fa-car
                    "Vehicle Issues" => "\uf071", // fa-exclamation-triangle
                    "Finish Day" => "\uf058",     // fa-check-circle
                    "Login" => "\uf2f6",          // fa-sign-in-alt
                    "Logout" => "\uf2f5",         // fa-sign-out-alt
                    "Register" => "\uf234",       // fa-user-plus
                    _ => "\uf111"                 // fa-circle (default)
                };
            }
            return "\uf111"; // fa-circle
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}

