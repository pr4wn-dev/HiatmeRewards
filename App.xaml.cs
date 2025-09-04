using Microsoft.Maui.Controls;
using Microsoft.Extensions.DependencyInjection;
using HiatMeApp.Models;

namespace HiatMeApp;

public partial class App : Application
{
    public static IServiceProvider Services { get; private set; }
    public static User? CurrentUser { get; set; } // Added for global user access

    public App(MauiApp mauiApp)
    {
        InitializeComponent();
        Services = mauiApp.Services;
        MainPage = new AppShell();
    }
}