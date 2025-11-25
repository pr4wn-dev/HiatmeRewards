using HiatMeApp.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui;
using Microsoft.Maui.Controls;

namespace HiatMeApp;

public partial class App : Application
{
public static IServiceProvider Services { get; private set; } = null!;
    public static User? CurrentUser { get; set; } // Added for global user access

    public App(MauiApp mauiApp)
    {
        InitializeComponent();
        Services = mauiApp.Services;
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        return new Window(new AppShell());
    }
}