using Android.Widget;
using Android.Views;
using Android.Util;
using Microsoft.Maui.Controls.Platform;
using Microsoft.Maui.Platform;
using System.Linq;
using System.Collections.Generic;
using System;
using System.Threading;
using System.Threading.Tasks;
using AView = Android.Views.View;
using AViewGroup = Android.Views.ViewGroup;

namespace HiatMeApp.Platforms.Android;

public static class MenuStyler
{
    private static int _maxRecursionDepth = 15; // Prevent infinite recursion - reduced for performance
    
    public static void StyleMenuItems(Shell shell)
    {
        try
        {
            // Only style if menu is actually open
            if (!shell.FlyoutIsPresented)
            {
                return;
            }
            
            if (shell?.Handler?.PlatformView is AView platformView)
            {
                // Post directly to UI thread - the call from AppShell is already delayed
                platformView.Post(() =>
                {
                    try
                    {
                        // Double-check menu is still open
                        if (!shell.FlyoutIsPresented)
                        {
                            return;
                        }
                        
                        // Find the flyout drawer view - it's usually a DrawerLayout or similar
                        // We need to find the actual flyout content, not the entire shell view
                        AView? flyoutView = FindFlyoutContentView(platformView);
                        
                        if (flyoutView != null)
                        {
                            // Remove all indicators first (with depth limit)
                            if (flyoutView is AViewGroup viewGroup)
                            {
                                RemoveAllIndicators(viewGroup, 0);
                            }
                            
                            // Then style and add indicators (with depth limit)
                            StyleMenuItemsRecursive(flyoutView, shell, 0);
                        }
                        else
                        {
                            // Fallback: remove indicators from entire view if we can't find flyout
                            if (platformView is AViewGroup viewGroup)
                            {
                                RemoveAllIndicators(viewGroup, 0);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"MenuStyler error in Post: {ex.Message}");
                    }
                });
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"MenuStyler error: {ex.Message}");
        }
    }
    
    private static AView? FindFlyoutContentView(AView rootView)
    {
        // Try to find the flyout drawer content
        // The flyout is typically in a DrawerLayout or similar container
        if (rootView is AViewGroup viewGroup)
        {
            int childCount = viewGroup.ChildCount;
            for (int i = 0; i < childCount; i++)
            {
                try
                {
                    var child = viewGroup.GetChildAt(i);
                    if (child != null)
                    {
                        var className = child.Class?.SimpleName ?? "";
                        // Look for drawer or flyout-related views
                        if (className.Contains("Drawer") || className.Contains("Flyout") || className.Contains("Navigation"))
                        {
                            // Check if this view contains menu items (has TextViews with menu text)
                            if (ContainsMenuItems(child))
                            {
                                return child;
                            }
                        }
                        
                        // Recurse into child
                        if (child is AViewGroup childGroup)
                        {
                            var found = FindFlyoutContentView(childGroup);
                            if (found != null)
                            {
                                return found;
                            }
                        }
                    }
                }
                catch
                {
                    // Ignore errors
                }
            }
        }
        return null;
    }
    
    private static bool ContainsMenuItems(AView view)
    {
        // Check if this view or its children contain menu item text
        if (view is TextView textView)
        {
            var text = textView.Text;
            if (!string.IsNullOrEmpty(text) && IsMenuItemText(text))
            {
                return true;
            }
        }
        
        if (view is AViewGroup viewGroup)
        {
            int childCount = viewGroup.ChildCount;
            for (int i = 0; i < childCount; i++)
            {
                try
                {
                    var child = viewGroup.GetChildAt(i);
                    if (child != null && ContainsMenuItems(child))
                    {
                        return true;
                    }
                }
                catch
                {
                    // Ignore errors
                }
            }
        }
        return false;
    }
    
    public static void RemoveAllIndicators(Shell shell)
    {
        try
        {
            if (shell?.Handler?.PlatformView is AView platformView)
            {
                platformView.Post(() =>
                {
                    try
                    {
                        if (platformView is AViewGroup viewGroup)
                        {
                            RemoveAllIndicators(viewGroup, 0);
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"MenuStyler error removing indicators: {ex.Message}");
                    }
                });
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"MenuStyler error: {ex.Message}");
        }
    }

    private static void RemoveAllIndicators(AView view, int depth)
    {
        if (depth > _maxRecursionDepth) return; // Prevent infinite recursion
        
        if (view is AViewGroup viewGroup)
        {
            // Remove background-based indicators first
            if (viewGroup.Tag?.ToString() == "has_selection_indicator")
            {
                viewGroup.Background = null;
                viewGroup.Tag = null;
            }
            
            // Remove all child view indicators
            int childCount = viewGroup.ChildCount;
            for (int i = childCount - 1; i >= 0; i--)
            {
                try
                {
                    var child = viewGroup.GetChildAt(i);
                    if (child?.Tag?.ToString() == "selection_indicator")
                    {
                        viewGroup.RemoveViewAt(i);
                    }
                    else if (child is AViewGroup childGroup)
                    {
                        RemoveAllIndicators(childGroup, depth + 1);
                    }
                }
                catch
                {
                    // Ignore errors on individual children
                }
            }
        }
    }

    private static void StyleMenuItemsRecursive(AView view, Shell shell, int depth)
    {
        if (depth > _maxRecursionDepth) return; // Prevent infinite recursion
        
        try
        {
            if (view is TextView textView)
            {
                // Check if this is a menu item text view
                var text = textView.Text;
                if (!string.IsNullOrEmpty(text) && IsMenuItemText(text))
                {
                    // Apply website-matching text styling
                    textView.SetTextColor(global::Android.Graphics.Color.ParseColor("#e2e8f0"));
                    textView.TextSize = 16; // Slightly smaller, more refined
                    textView.Typeface = global::Android.Graphics.Typeface.Create("sans-serif-medium", global::Android.Graphics.TypefaceStyle.Normal);
                    
                    // Get the parent container (usually a LinearLayout or similar)
                    var parent = textView.Parent as AViewGroup;
                    if (parent != null)
                    {
                        // Find the root container for this menu item - prefer LayoutViewGroup with reasonable height
                        var rootContainer = FindMenuItemRootContainer(parent);
                        if (rootContainer != null)
                        {
                            // Only proceed if this looks like a menu item row (not a parent container)
                            var containerHeight = rootContainer.Height;
                            var containerClass = rootContainer.Class?.SimpleName ?? "";
                            
                            // Skip containers that are too tall (likely parent containers, not menu item rows)
                            if (containerHeight > 0 && containerHeight > 300)
                            {
                                return; // Continue to next view - removed debug logging for performance
                            }
                            
                            // Check if this menu item corresponds to the current route
                            string? currentRoute = GetCurrentRoute(shell);
                            bool isSelected = IsMenuItemSelected(text, currentRoute);
                            
                            // Apply selection indicator if selected
                            if (isSelected)
                            {
                                AddSelectionIndicator(rootContainer, text);
                            }
                        }
                    }
                }
            }

            if (view is AViewGroup viewGroup)
            {
                int childCount = viewGroup.ChildCount;
                for (int i = 0; i < childCount; i++)
                {
                    try
                    {
                        var child = viewGroup.GetChildAt(i);
                        if (child != null)
                        {
                            StyleMenuItemsRecursive(child, shell, depth + 1);
                        }
                    }
                    catch
                    {
                        // Ignore errors on individual children
                    }
                }
            }
        }
        catch
        {
            // Ignore errors to prevent blocking
        }
    }
    
    private static AViewGroup? FindMenuItemRootContainer(AViewGroup parent)
    {
        // The menu item container is usually a LinearLayout or FrameLayout
        // that contains the text view. We need to find the right level.
        // Prefer containers with reasonable height (menu item rows are typically 48-72dp)
        var current = parent;
        int depth = 0;
        const int maxDepth = 6;
        AViewGroup? bestContainer = null;
        int bestScore = 0;
        
        while (current != null && depth < maxDepth)
        {
            var className = current.Class?.SimpleName;
            // Look for LayoutViewGroup or similar - these are usually the menu item rows
            if (className != null && 
                !className.Contains("RecyclerView") && // Skip RecyclerView itself
                (className.Contains("LayoutViewGroup") || // This is usually the menu item row
                 className.Contains("LinearLayout") || 
                 className.Contains("FrameLayout") ||
                 className.Contains("RelativeLayout")))
            {
                // Check if this container has a reasonable structure
                if (current.ChildCount >= 1 && current.ChildCount <= 5)
                {
                    try
                    {
                        var width = current.Width;
                        var height = current.Height;
                        
                        // Score containers: prefer LayoutViewGroup with reasonable height (48-200dp)
                        int score = 0;
                        if (width > 200) score += 10; // Wide enough
                        if (height > 0 && height < 600) // Reasonable height (not too tall)
                        {
                            score += 20;
                            if (height >= 100 && height <= 200) score += 10; // Ideal height range
                        }
                        if (className.Contains("LayoutViewGroup")) score += 30; // Prefer LayoutViewGroup
                        
                        if (score > bestScore)
                        {
                            bestScore = score;
                            bestContainer = current;
                            System.Diagnostics.Debug.WriteLine($"MenuStyler: Found better container '{className}' with {current.ChildCount} children, width={width}, height={height}, score={score}");
                        }
                    }
                    catch { }
                }
            }
            
            current = current.Parent as AViewGroup;
            depth++;
        }
        
        // Return the best container we found, or fall back to immediate parent
        if (bestContainer != null)
        {
            System.Diagnostics.Debug.WriteLine($"MenuStyler: Using best container '{bestContainer.Class?.SimpleName}' with score={bestScore}");
            return bestContainer;
        }
        
        System.Diagnostics.Debug.WriteLine($"MenuStyler: Using fallback parent container '{parent.Class?.SimpleName}'");
        return parent;
    }

    private static void RemoveSelectionIndicators(AViewGroup parent)
    {
        // Remove background-based indicators
        if (parent.Tag?.ToString() == "has_selection_indicator")
        {
            parent.Background = null;
            parent.Tag = null;
        }
        
        // Remove all child view indicators
        for (int i = parent.ChildCount - 1; i >= 0; i--)
        {
            var child = parent.GetChildAt(i);
            if (child?.Tag?.ToString() == "selection_indicator")
            {
                parent.RemoveViewAt(i);
            }
        }
    }

    private static void AddSelectionIndicator(AViewGroup parent, string menuText = "")
    {
        try
        {
            // Remove any existing indicator first (should already be done, but double-check)
            if (parent.Tag?.ToString() == "has_selection_indicator")
            {
                parent.Background = null;
                parent.Tag = null;
            }
            
            // Convert 4dp to pixels (thinner accent bar like website)
            var displayMetrics = Platform.CurrentActivity.Resources?.DisplayMetrics;
            float density = displayMetrics != null ? displayMetrics.Density : 2.0f;
            int widthPx = (int)(4 * density);
            
            // Get parent dimensions
            int parentWidth = parent.Width;
            
            // Get color based on menu item (matching website color scheme)
            string colorHex = GetMenuItemColor(menuText);
            var accentColor = global::Android.Graphics.Color.ParseColor(colorHex);
            
            // Create a subtle background highlight with the accent color
            var bgColor = global::Android.Graphics.Color.ParseColor("#1a3a5c"); // Subtle blue tint
            var background = new global::Android.Graphics.Drawables.ColorDrawable(bgColor);
            var leftBar = new global::Android.Graphics.Drawables.ColorDrawable(accentColor);
            
            var layers = new global::Android.Graphics.Drawables.Drawable[] { background, leftBar };
            var layerList = new global::Android.Graphics.Drawables.LayerDrawable(layers);
            
            // Set the left bar to be 4dp wide on the left side
            layerList.SetLayerWidth(1, widthPx);
            layerList.SetLayerGravity(1, global::Android.Views.GravityFlags.Left | global::Android.Views.GravityFlags.FillVertical);
            layerList.SetLayerInsetLeft(1, 0);
            layerList.SetLayerInsetRight(1, parentWidth > 0 ? parentWidth - widthPx : 0);
            layerList.SetLayerInsetTop(1, 0);
            layerList.SetLayerInsetBottom(1, 0);
            
            // Set as background
            parent.Background = layerList;
            
            // Mark parent so we can remove it later
            parent.Tag = "has_selection_indicator";
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"MenuStyler: Error adding indicator: {ex.Message}");
        }
    }
    
    private static string GetMenuItemColor(string menuText)
    {
        // Return accent colors matching website menu-item-* classes
        return menuText switch
        {
            "Home" => "#3b82f6",           // Blue
            "Profile" => "#8b5cf6",        // Purple
            "Request Day Off" => "#8b5cf6", // Purple
            "Vehicle" => "#14b8a6",        // Teal
            "Vehicle Issues" => "#f97316", // Orange
            "Finish Day" => "#22c55e",     // Green
            "Login" => "#3b82f6",          // Blue
            "Logout" => "#ef4444",         // Red
            "Register" => "#f59e0b",       // Gold
            _ => "#3b82f6"                 // Default blue
        };
    }

    private static string? GetCurrentRoute(Shell shell)
    {
        try
        {
            // Try to get the current route efficiently
            var currentState = shell.CurrentState;
            if (currentState != null)
            {
                var location = currentState.Location;
                if (location != null)
                {
                    // First try: Get the full path and extract the route
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
        catch
        {
            // Ignore errors - removed debug logging for performance
        }
        return null;
    }

    private static bool IsMenuItemSelected(string menuText, string? currentRoute)
    {
        if (string.IsNullOrEmpty(currentRoute))
            return false;
            
        // Map menu text to routes
        // Note: Logout should never be selected - it's an action, not a page
        var routeMap = new Dictionary<string, string>
        {
            { "Home", "Home" },
            { "Profile", "Profile" },
            { "Request Day Off", "RequestDayOff" },
            { "Vehicle", "Vehicle" },
            { "Vehicle Issues", "VehicleIssues" },
            { "Finish Day", "FinishDay" },
            { "Login", "Login" },
            { "Register", "Register" }
            // Logout is intentionally not in the map - it should never show as selected
        };
        
        if (routeMap.TryGetValue(menuText, out var route))
        {
            return route.Equals(currentRoute, StringComparison.OrdinalIgnoreCase);
        }
        
        return false; // Logout and other unmapped items will return false
    }

    private static bool IsMenuItemText(string text)
    {
        var menuTexts = new[] { "Home", "Profile", "Request Day Off", "Vehicle", "Vehicle Issues", "Finish Day", "Login", "Logout", "Register" };
        return menuTexts.Contains(text);
    }
}

