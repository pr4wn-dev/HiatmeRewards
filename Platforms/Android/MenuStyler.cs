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
            if (shell?.Handler?.PlatformView is AView platformView)
            {
                // Post directly to UI thread - the call from AppShell is already delayed
                platformView.Post(() =>
                {
                    try
                    {
                        // Remove all indicators first (with depth limit)
                        if (platformView is AViewGroup viewGroup)
                        {
                            RemoveAllIndicators(viewGroup, 0);
                        }
                        
                        // Then style and add indicators (with depth limit)
                        StyleMenuItemsRecursive(platformView, shell, 0);
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

    private static void RemoveAllIndicators(AView view, int depth)
    {
        if (depth > _maxRecursionDepth) return; // Prevent infinite recursion
        
        if (view is AViewGroup viewGroup)
        {
            // Remove all indicators from this view group
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
                    textView.SetTextColor(global::Android.Graphics.Color.White);
                    textView.TextSize = 20; // 20sp - larger font size
                    textView.Typeface = global::Android.Graphics.Typeface.Default;
                    
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
                                System.Diagnostics.Debug.WriteLine($"MenuStyler: Skipping container '{containerClass}' with height={containerHeight} (too tall for menu item)");
                                return; // Continue to next view
                            }
                            
                            // Check if this menu item corresponds to the current route
                            string? currentRoute = GetCurrentRoute(shell);
                            bool isSelected = IsMenuItemSelected(text, currentRoute);
                            
                            // Debug logging
                            System.Diagnostics.Debug.WriteLine($"MenuStyler: MenuItem='{text}', CurrentRoute='{currentRoute}', IsSelected={isSelected}, Container='{containerClass}', ContainerWidth={rootContainer.Width}, ContainerHeight={containerHeight}");
                            
                            // Only add indicator if selected (removal already done at top level)
                            if (isSelected)
                            {
                                System.Diagnostics.Debug.WriteLine($"MenuStyler: Adding indicator for '{text}' to container '{containerClass}' (height={containerHeight})");
                                AddSelectionIndicator(rootContainer);
                            }
                        }
                        else
                        {
                            System.Diagnostics.Debug.WriteLine($"MenuStyler: Could not find root container for '{text}'");
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
        // Remove all existing selection indicators (both child views and background)
        for (int i = parent.ChildCount - 1; i >= 0; i--)
        {
            var child = parent.GetChildAt(i);
            if (child?.Tag?.ToString() == "selection_indicator")
            {
                parent.RemoveViewAt(i);
            }
        }
        
        // Also remove background-based indicators
        if (parent.Tag?.ToString() == "has_selection_indicator")
        {
            parent.Background = null;
            parent.Tag = null;
        }
    }

    private static void AddSelectionIndicator(AViewGroup parent)
    {
        try
        {
            // Check if indicator already exists by looking for the view
            for (int i = 0; i < parent.ChildCount; i++)
            {
                var child = parent.GetChildAt(i);
                if (child?.Tag?.ToString() == "selection_indicator")
                {
                    // Check if it has valid dimensions
                    if (child.Width > 0 && child.Height > 0)
                    {
                        System.Diagnostics.Debug.WriteLine($"MenuStyler: Indicator already exists with valid dimensions (width={child.Width}, height={child.Height})");
                        return;
                    }
                    else
                    {
                        // Remove invalid indicator
                        System.Diagnostics.Debug.WriteLine($"MenuStyler: Removing invalid indicator (width={child.Width}, height={child.Height})");
                        parent.RemoveViewAt(i);
                        break;
                    }
                }
            }
            
            // Convert 14dp to pixels
            var displayMetrics = Platform.CurrentActivity.Resources?.DisplayMetrics;
            float density = displayMetrics != null ? displayMetrics.Density : 2.0f;
            int widthPx = (int)(14 * density);
            
            // Get parent dimensions for debugging
            int parentWidth = parent.Width;
            int parentHeight = parent.Height;
            System.Diagnostics.Debug.WriteLine($"MenuStyler: Parent dimensions: width={parentWidth}, height={parentHeight}, class={parent.Class?.SimpleName}");
            
            // Try using a background drawable approach instead of adding child views
            // LayoutViewGroup doesn't seem to support adding child views properly
            var color = global::Android.Graphics.Color.ParseColor("#007bff");
            
            // Use LayerDrawable to create a transparent background with a blue left bar
            var transparentBg = new global::Android.Graphics.Drawables.ColorDrawable(global::Android.Graphics.Color.Transparent);
            var leftBar = new global::Android.Graphics.Drawables.ColorDrawable(color);
            
            var layers = new global::Android.Graphics.Drawables.Drawable[] { transparentBg, leftBar };
            var layerList = new global::Android.Graphics.Drawables.LayerDrawable(layers);
            
            // Set the left bar to be 14dp wide on the left side
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
            
            System.Diagnostics.Debug.WriteLine($"MenuStyler: Set background drawable with left border (width={widthPx}px) on parent '{parent.Class?.SimpleName}'");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"MenuStyler: Error adding indicator: {ex.Message}\n{ex.StackTrace}");
        }
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
                    // First try: Get the full path and extract the route
                    var fullPath = location.ToString();
                    System.Diagnostics.Debug.WriteLine($"MenuStyler: FullPath='{fullPath}'");
                    
                    if (!string.IsNullOrEmpty(fullPath))
                    {
                        // Remove leading slashes and get the last segment
                        var segments = fullPath.TrimStart('/').Split('/');
                        if (segments.Length > 0 && !string.IsNullOrEmpty(segments[segments.Length - 1]))
                        {
                            var route = segments[segments.Length - 1];
                            System.Diagnostics.Debug.WriteLine($"MenuStyler: Extracted route from path: '{route}'");
                            return route;
                        }
                    }
                    
                    // Fallback: try Segments property
                    if (location.Segments != null)
                    {
                        var segments = location.Segments.ToList();
                        if (segments.Count > 0)
                        {
                            var route = segments[segments.Count - 1];
                            System.Diagnostics.Debug.WriteLine($"MenuStyler: Extracted route from Segments: '{route}'");
                            return route;
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
                    System.Diagnostics.Debug.WriteLine($"MenuStyler: Route from CurrentItem: '{route}'");
                    return route;
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"MenuStyler: Error getting route: {ex.Message}");
        }
        System.Diagnostics.Debug.WriteLine("MenuStyler: No route found");
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

