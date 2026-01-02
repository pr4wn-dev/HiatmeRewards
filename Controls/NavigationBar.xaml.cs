using Microsoft.Maui.Controls;

namespace HiatMeApp.Controls;

public partial class NavigationBar : ContentView
{
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
}