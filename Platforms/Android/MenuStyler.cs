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
                        // Find the root container for this menu item (usually 2-3 levels up)
                        var rootContainer = FindMenuItemRootContainer(parent);
                        if (rootContainer != null)
                        {
                            // Check if this menu item corresponds to the current route
                            string? currentRoute = GetCurrentRoute(shell);
                            bool isSelected = IsMenuItemSelected(text, currentRoute);
                            
                            // Debug logging
                            System.Diagnostics.Debug.WriteLine($"MenuStyler: MenuItem='{text}', CurrentRoute='{currentRoute}', IsSelected={isSelected}, Container='{rootContainer.Class?.SimpleName}', ContainerWidth={rootContainer.Width}, ContainerHeight={rootContainer.Height}");
                            
                            // Only add indicator if selected (removal already done at top level)
                            if (isSelected)
                            {
                                System.Diagnostics.Debug.WriteLine($"MenuStyler: Adding indicator for '{text}' to container '{rootContainer.Class?.SimpleName}'");
                                AddSelectionIndicator(rootContainer);
                                
                                // Also try setting background color on the container itself as a fallback
                                try
                                {
                                    // Set a left padding or background to make indicator visible
                                    rootContainer.SetPadding(0, 0, 0, 0); // Reset padding
                                }
                                catch { }
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
        // Try to find a container that's wide enough to hold the indicator on the left
        var current = parent;
        int depth = 0;
        const int maxDepth = 6; // Increased to find the right container
        AViewGroup? bestContainer = null;
        int bestWidth = 0;
        
        while (current != null && depth < maxDepth)
        {
            var className = current.Class?.SimpleName;
            // Look for common container types that would hold a menu item
            if (className != null && 
                !className.Contains("RecyclerView") && // Skip RecyclerView itself
                (className.Contains("LinearLayout") || 
                 className.Contains("FrameLayout") ||
                 className.Contains("RelativeLayout") ||
                 className.Contains("ViewGroup")))
            {
                // Check if this container has a reasonable structure
                // Menu items typically have 1-5 children (icon, text, maybe other elements)
                if (current.ChildCount >= 1 && current.ChildCount <= 5)
                {
                    // Check if this container has a reasonable width (not too small)
                    try
                    {
                        var width = current.Width;
                        if (width > 200) // Has been measured and is wide enough (menu items should be at least 200px)
                        {
                            if (width > bestWidth)
                            {
                                bestWidth = width;
                                bestContainer = current;
                                System.Diagnostics.Debug.WriteLine($"MenuStyler: Found better container '{className}' with {current.ChildCount} children, width={width}");
                            }
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
            System.Diagnostics.Debug.WriteLine($"MenuStyler: Using best container '{bestContainer.Class?.SimpleName}' with width={bestWidth}");
            return bestContainer;
        }
        
        System.Diagnostics.Debug.WriteLine($"MenuStyler: Using fallback parent container '{parent.Class?.SimpleName}'");
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
        try
        {
            // Check if indicator already exists (by checking tag on parent)
            if (parent.Tag?.ToString() == "has_indicator")
            {
                System.Diagnostics.Debug.WriteLine("MenuStyler: Container already marked as having indicator");
                return;
            }
            
            // Convert 14dp to pixels
            var displayMetrics = Platform.CurrentActivity.Resources?.DisplayMetrics;
            float density = displayMetrics != null ? displayMetrics.Density : 2.0f;
            int widthPx = (int)(14 * density);
            
            // Get parent dimensions for debugging
            int parentWidth = parent.Width;
            int parentHeight = parent.Height;
            System.Diagnostics.Debug.WriteLine($"MenuStyler: Parent dimensions: width={parentWidth}, height={parentHeight}, class={parent.Class?.SimpleName}");
            
            // Disable clipping on parent to ensure indicator is visible
            parent.SetClipChildren(false);
            parent.SetClipToPadding(false);
            
            // Try approach 1: Create a drawable background with left border
            try
            {
                var color = global::Android.Graphics.Color.ParseColor("#007bff");
                var drawable = new global::Android.Graphics.Drawables.GradientDrawable();
                drawable.SetColor(global::Android.Graphics.Color.Transparent);
                
                // Create a shape drawable for the left border
                var shape = new global::Android.Graphics.Drawables.ShapeDrawable(new global::Android.Graphics.Drawables.Shapes.RectShape());
                var paint = shape.Paint;
                paint.Color = color;
                paint.StrokeWidth = widthPx;
                paint.SetStyle(global::Android.Graphics.Paint.Style.Fill);
                
                // Use a layer list to combine transparent background with left border
                var layerList = new global::Android.Graphics.Drawables.LayerDrawable(new global::Android.Graphics.Drawables.Drawable[] { drawable });
                // Create a left border by adding padding/drawable
                
                // Simpler: Just set left padding and use a colored view
                parent.SetPadding(widthPx, 0, 0, 0);
            }
            catch { }
            
            // Approach 2: Add a child view as indicator
            // Create blue rectangle indicator
            var indicatorView = new AView(Platform.CurrentActivity);
            indicatorView.SetBackgroundColor(global::Android.Graphics.Color.ParseColor("#007bff")); // WebsiteAccent blue
            indicatorView.Tag = "selection_indicator";
            indicatorView.SetPadding(0, 0, 0, 0);
            
            // Use explicit dimensions
            int height = parentHeight > 0 ? parentHeight : ViewGroup.LayoutParams.MatchParent;
            
            // Create layout params - use absolute positioning
            var layoutParams = new ViewGroup.MarginLayoutParams(widthPx, height);
            layoutParams.LeftMargin = 0;
            layoutParams.TopMargin = 0;
            
            // Ensure the view is visible
            indicatorView.Visibility = ViewStates.Visible;
            indicatorView.SetMinimumWidth(widthPx);
            indicatorView.SetMinimumHeight(height > 0 ? height : (int)(56 * density));
            
            // Add as first child (left side)
            parent.AddView(indicatorView, 0, layoutParams);
            
            // Mark parent as having indicator
            parent.Tag = "has_indicator";
            
            // Force layout
            indicatorView.RequestLayout();
            parent.RequestLayout();
            
            // Verify after layout
            indicatorView.Post(() =>
            {
                try
                {
                    var actualWidth = indicatorView.Width;
                    var actualHeight = indicatorView.Height;
                    var bounds = new global::Android.Graphics.Rect();
                    indicatorView.GetDrawingRect(bounds);
                    System.Diagnostics.Debug.WriteLine($"MenuStyler: Indicator verification - width={actualWidth}, height={actualHeight}, bounds=({bounds.Left},{bounds.Top})-({bounds.Right},{bounds.Bottom}), visible={indicatorView.Visibility == ViewStates.Visible}");
                }
                catch { }
            });
            
            System.Diagnostics.Debug.WriteLine($"MenuStyler: Added indicator view (width={widthPx}px, height={height}, density={density}) to parent '{parent.Class?.SimpleName}'");
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

