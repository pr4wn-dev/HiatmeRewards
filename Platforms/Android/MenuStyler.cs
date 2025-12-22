using Android.Widget;
using Android.Views;
using Microsoft.Maui.Controls.Platform;
using Microsoft.Maui.Platform;
using System.Linq;
using AView = Android.Views.View;
using AViewGroup = Android.Views.ViewGroup;

namespace HiatMeApp.Platforms.Android;

public static class MenuStyler
{
    public static void StyleMenuItems(Shell shell)
    {
        if (shell?.Handler?.PlatformView is AView platformView)
        {
            // Use a delayed action to allow the menu to render first
            platformView.Post(() =>
            {
                StyleMenuItemsRecursive(platformView);
            });
        }
    }

    private static void StyleMenuItemsRecursive(AView view)
    {
        if (view is TextView textView)
        {
            // Check if this is a menu item text view
            var text = textView.Text;
            if (!string.IsNullOrEmpty(text) && IsMenuItemText(text))
            {
                textView.SetTextColor(global::Android.Graphics.Color.White);
                textView.TextSize = 20; // 20sp - larger font size
                textView.Typeface = global::Android.Graphics.Typeface.Default;
            }
        }

        if (view is AViewGroup viewGroup)
        {
            for (int i = 0; i < viewGroup.ChildCount; i++)
            {
                var child = viewGroup.GetChildAt(i);
                StyleMenuItemsRecursive(child);
            }
        }
    }

    private static bool IsMenuItemText(string text)
    {
        var menuTexts = new[] { "Home", "Profile", "Request Day Off", "Vehicle", "Vehicle Issues", "Finish Day", "Login", "Logout", "Register" };
        return menuTexts.Contains(text);
    }
}

