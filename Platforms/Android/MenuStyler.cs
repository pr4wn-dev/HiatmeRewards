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
    private static AViewGroup? _lastFlyoutContentView = null;
    
    public static void StyleMenuItems(Shell shell)
    {
        if (shell?.Handler?.PlatformView is AView platformView)
        {
            // Find the flyout content view instead of traversing entire shell
            AViewGroup? flyoutContentView = FindFlyoutContentView(platformView);
            if (flyoutContentView == null)
            {
                // Fallback to full traversal if flyout not found
                flyoutContentView = platformView as AViewGroup;
            }
            
            if (flyoutContentView != null)
            {
                _lastFlyoutContentView = flyoutContentView;
                
                // Use a delayed action to allow the menu to render first
                platformView.Post(() =>
                {
                    // Remove all indicators first
                    RemoveAllIndicators(flyoutContentView);
                    
                    // Then style and add indicators
                    StyleMenuItemsRecursive(flyoutContentView, shell);
                });
            }
        }
    }
    
    private static AViewGroup? FindFlyoutContentView(AView view)
    {
        // Try to find the flyout content view by traversing the hierarchy
        if (view is AViewGroup viewGroup)
        {
            // Look for common flyout container class names or IDs
            for (int i = 0; i < viewGroup.ChildCount; i++)
            {
                var child = viewGroup.GetChildAt(i);
                if (child is AViewGroup childGroup)
                {
                    // Check if this looks like a flyout content view
                    var className = childGroup.Class.SimpleName;
                    if (className != null && (className.Contains("Flyout") || className.Contains("Drawer")))
                    {
                        return childGroup;
                    }
                    
                    // Recursively search
                    var found = FindFlyoutContentView(childGroup);
                    if (found != null)
                        return found;
                }
            }
        }
        return null;
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
                
                // Get the parent container (usually a LinearLayout or similar)
                var parent = textView.Parent as AViewGroup;
                if (parent != null)
                {
                    // Find the root container for this menu item (usually 2-3 levels up)
                    var rootContainer = FindMenuItemRootContainer(parent);
                    if (rootContainer != null)
                    {
                        // Check if this menu item corresponds to the current route
                        string? currentRoute = GetCurrentRoute(shell);
                        bool isSelected = IsMenuItemSelected(text, currentRoute);
                        
                        // Only add indicator if selected (removal already done at top level)
                        if (isSelected)
                        {
                            AddSelectionIndicator(rootContainer);
                        }
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
    
    private static AViewGroup? FindMenuItemRootContainer(AViewGroup parent)
    {
        // The menu item container is usually a LinearLayout or FrameLayout
        // that contains the text view. We need to find the right level.
        var current = parent;
        int depth = 0;
        const int maxDepth = 5; // Prevent infinite loops
        
        while (current != null && depth < maxDepth)
        {
            var className = current.Class.SimpleName;
            // Look for common container types that would hold a menu item
            if (className != null && 
                (className.Contains("LinearLayout") || 
                 className.Contains("FrameLayout") ||
                 className.Contains("RelativeLayout")))
            {
                // Check if this container has the right structure (text view + possibly icon)
                if (current.ChildCount >= 1 && current.ChildCount <= 3)
                {
                    return current;
                }
            }
            
            current = current.Parent as AViewGroup;
            depth++;
        }
        
        // Fallback: return the immediate parent if we can't find a better container
        return parent;
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
            // Try multiple methods to get the current route
            var currentState = shell.CurrentState;
            if (currentState != null)
            {
                var location = currentState.Location;
                if (location != null)
                {
                    // Get the full path and extract the route
                    var fullPath = location.ToString();
                    if (!string.IsNullOrEmpty(fullPath))
                    {
                        // Remove leading slashes and get the last segment
                        var segments = fullPath.TrimStart('/').Split('/');
                        if (segments.Length > 0 && !string.IsNullOrEmpty(segments[segments.Length - 1]))
                        {
                            return segments[segments.Length - 1];
                        }
                    }
                    
                    // Fallback: try Segments property
                    if (location.Segments != null)
                    {
                        var segments = location.Segments.ToList();
                        if (segments.Count > 0)
                        {
                            return segments[segments.Count - 1];
                        }
                    }
                }
            }
            
            // Additional fallback: check current item
            var currentItem = shell.CurrentItem;
            if (currentItem != null)
            {
                var route = currentItem.Route;
                if (!string.IsNullOrEmpty(route))
                {
                    return route;
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

