using Microsoft.Maui.Controls;

namespace HiatMeApp.Controls;

public partial class TopBar : ContentView
{
    public TopBar()
    {
        InitializeComponent();
    }

    private void OnMenuButtonClicked(object sender, EventArgs e)
    {
        if (Shell.Current != null)
        {
            Shell.Current.FlyoutIsPresented = !Shell.Current.FlyoutIsPresented;
        }
    }
}

