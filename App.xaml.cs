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
        
        // Add global exception handlers
        AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
        TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        var window = new Window(new AppShell());
        
        // Handle window-level exceptions
        window.Created += (s, e) => Console.WriteLine("Window: Created");
        window.Activated += (s, e) => Console.WriteLine("Window: Activated");
        window.Deactivated += (s, e) => Console.WriteLine("Window: Deactivated");
        window.Stopped += (s, e) => Console.WriteLine("Window: Stopped");
        window.Destroying += (s, e) => Console.WriteLine("Window: Destroying");
        
        return window;
    }
    
    private void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        var exception = e.ExceptionObject as Exception;
        Console.WriteLine($"App: Unhandled Exception: {exception?.Message}");
        Console.WriteLine($"App: StackTrace: {exception?.StackTrace}");
        
        // Try to save to a file or log for debugging
        try
        {
            var logMessage = $"[{DateTime.Now}] Unhandled Exception: {exception?.Message}\nStackTrace: {exception?.StackTrace}\n\n";
            System.Diagnostics.Debug.WriteLine(logMessage);
        }
        catch { }
    }
    
    private void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
    {
        Console.WriteLine($"App: Unobserved Task Exception: {e.Exception?.Message}");
        Console.WriteLine($"App: StackTrace: {e.Exception?.StackTrace}");
        e.SetObserved(); // Mark as handled to prevent app crash
    }
}