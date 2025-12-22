using Android.Widget;
using Android.Views;
using Android.Util;
using Microsoft.Maui.Controls.Platform;
using Microsoft.Maui.Platform;
using System.Linq;
using System.Collections.Generic;
using System;
using AView = Android.Views.View;
using AViewGroup = Android.Views.ViewGroup;

namespace HiatMeApp.Platforms.Android;

public static class MenuStyler
{
    public static void StyleMenuItems(Shell shell)
    {
        if (shell?.Handler?.PlatformView is AView platformView)
        {
            // First, remove all existing indicators
            RemoveAllIndicators(platformView);
            
            // Use a delayed action to allow the menu to render first
            platformView.Post(() =>
            {
                StyleMenuItemsRecursive(platformView, shell);
            });
        }
    }

    private static void RemoveAllIndicators(AView view)
    {
        if (view is AViewGroup viewGroup)
        {
            // Remove all indicators from this view group
            for (int i = viewGroup.ChildCount - 1; i >= 0; i--)
            {
                var child = viewGroup.GetChildAt(i);
                if (child.Tag?.ToString() == "selection_indicator")
                {
                    viewGroup.RemoveViewAt(i);
                }
                else if (child is AViewGroup childGroup)
                {
                    RemoveAllIndicators(childGroup);
                }
            }
        }
    }

    private static void StyleMenuItemsRecursive(AView view, Shell shell)
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
                
                // Get the parent to manage indicators
                var parent = textView.Parent as AViewGroup;
                if (parent != null)
                {
                    // Remove any existing indicators first
                    RemoveSelectionIndicators(parent);
                    
                    // Check if this menu item corresponds to the current route
                    string? currentRoute = GetCurrentRoute(shell);
                    bool isSelected = IsMenuItemSelected(text, currentRoute);
                    
                    // Add blue rectangle indicator if selected
                    if (isSelected)
                    {
                        AddSelectionIndicator(parent);
                    }
                }
            }
        }

        if (view is AViewGroup viewGroup)
        {
            for (int i = 0; i < viewGroup.ChildCount; i++)
            {
                var child = viewGroup.GetChildAt(i);
                StyleMenuItemsRecursive(child, shell);
            }
        }
    }

    private static void RemoveSelectionIndicators(AViewGroup parent)
    {
        // Remove all existing selection indicators
        for (int i = parent.ChildCount - 1; i >= 0; i--)
        {
            var child = parent.GetChildAt(i);
            if (child.Tag?.ToString() == "selection_indicator")
            {
                parent.RemoveViewAt(i);
            }
        }
    }

    private static void AddSelectionIndicator(AViewGroup parent)
    {
        // Create blue rectangle indicator
        var indicatorView = new AView(Platform.CurrentActivity);
        indicatorView.SetBackgroundColor(global::Android.Graphics.Color.ParseColor("#007bff")); // WebsiteAccent blue
        indicatorView.Tag = "selection_indicator";
        
        // Add as first child (left side) with proper layout params
        // Convert 14dp to pixels
        var displayMetrics = Platform.CurrentActivity.Resources?.DisplayMetrics;
        int widthPx = displayMetrics != null ? (int)(14 * displayMetrics.Density) : (int)(14 * 2); // Default to 2x density if null
        var layoutParams = new ViewGroup.MarginLayoutParams(
            widthPx, // 14dp width (matching header) in pixels
            ViewGroup.LayoutParams.MatchParent // Full height
        );
        parent.AddView(indicatorView, 0, layoutParams);
    }

    private static string? GetCurrentRoute(Shell shell)
    {
        try
        {
            var currentState = shell.CurrentState;
            if (currentState != null)
            {
                var location = currentState.Location;
                if (location != null && location.Segments != null)
                {
                    var segments = location.Segments.ToList();
                    if (segments.Count > 0)
                    {
                        return segments[segments.Count - 1];
                    }
                }
            }
        }
        catch { }
        return null;
    }

    private static bool IsMenuItemSelected(string menuText, string? currentRoute)
    {
        if (string.IsNullOrEmpty(currentRoute))
            return false;
            
        // Map menu text to routes
        var routeMap = new Dictionary<string, string>
        {
            { "Home", "Home" },
            { "Profile", "Profile" },
            { "Request Day Off", "RequestDayOff" },
            { "Vehicle", "Vehicle" },
            { "Vehicle Issues", "VehicleIssues" },
            { "Finish Day", "FinishDay" },
            { "Login", "Login" },
            { "Logout", "Home" }, // Logout shows Home after logout
            { "Register", "Register" }
        };
        
        if (routeMap.TryGetValue(menuText, out var route))
        {
            return route.Equals(currentRoute, StringComparison.OrdinalIgnoreCase);
        }
        
        return false;
    }

    private static bool IsMenuItemText(string text)
    {
        var menuTexts = new[] { "Home", "Profile", "Request Day Off", "Vehicle", "Vehicle Issues", "Finish Day", "Login", "Logout", "Register" };
        return menuTexts.Contains(text);
    }
}

