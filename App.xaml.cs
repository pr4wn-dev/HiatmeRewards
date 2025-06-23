using Microsoft.Maui.Controls;

namespace HiatMeApp;

public partial class App : Application
{
    public App()
    {
        InitializeComponent();
        MainPage = new AppShell();
    }
}