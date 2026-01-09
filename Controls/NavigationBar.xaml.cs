using Microsoft.Maui.Controls;

namespace HiatMeApp.Controls;

public partial class NavigationBar : ContentView
{
    private const double ScrollAmount = 150; // Pixels to scroll per arrow click
    
    public NavigationBar()
    {
        try
        {
            InitializeComponent();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"NavigationBar: InitializeComponent error: {ex.Message}");
        }
    }
    
    protected override void OnBindingContextChanged()
    {
        base.OnBindingContextChanged();
        // NavigationBar can work without BindingContext - buttons just won't do anything
    }
    
    private async void OnLeftArrowClicked(object? sender, EventArgs e)
    {
        try
        {
            // Scroll left by ScrollAmount pixels
            var currentScrollX = NavScrollView.ScrollX;
            var newScrollX = Math.Max(0, currentScrollX - ScrollAmount);
            await NavScrollView.ScrollToAsync(newScrollX, 0, true);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"NavigationBar: OnLeftArrowClicked error: {ex.Message}");
        }
    }
    
    private async void OnRightArrowClicked(object? sender, EventArgs e)
    {
        try
        {
            // Scroll right by ScrollAmount pixels
            var currentScrollX = NavScrollView.ScrollX;
            var maxScrollX = NavScrollView.ContentSize.Width - NavScrollView.Width;
            var newScrollX = Math.Min(maxScrollX, currentScrollX + ScrollAmount);
            await NavScrollView.ScrollToAsync(newScrollX, 0, true);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"NavigationBar: OnRightArrowClicked error: {ex.Message}");
        }
    }
}
