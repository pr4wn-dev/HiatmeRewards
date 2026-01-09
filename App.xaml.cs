using HiatMeApp.Models;
using HiatMeApp.Services;
using HiatMeApp.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Storage;
using Newtonsoft.Json;

namespace HiatMeApp;

public partial class App : Application
{
    public static IServiceProvider Services { get; private set; } = null!;
    public static User? CurrentUser { get; set; } // Added for global user access
    
    // Track if we need to validate session on resume
    private static bool _wasBackgrounded = false;
    private static DateTime _backgroundedAt = DateTime.MinValue;
    
    // How long before we force revalidation (5 minutes)
    private static readonly TimeSpan SessionRevalidationThreshold = TimeSpan.FromMinutes(5);

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
        
        // Handle window-level lifecycle events
        window.Created += (s, e) => Console.WriteLine("Window: Created");
        window.Activated += OnWindowActivated;
        window.Deactivated += OnWindowDeactivated;
        window.Stopped += OnWindowStopped;
        window.Resumed += OnWindowResumed;
        window.Destroying += (s, e) => Console.WriteLine("Window: Destroying");
        
        return window;
    }
    
    private void OnWindowDeactivated(object? sender, EventArgs e)
    {
        Console.WriteLine("Window: Deactivated - app going to background");
        _wasBackgrounded = true;
        _backgroundedAt = DateTime.UtcNow;
        
        // Save current state before going to background
        SaveAppState();
    }
    
    private void OnWindowStopped(object? sender, EventArgs e)
    {
        Console.WriteLine("Window: Stopped - app fully backgrounded");
        _wasBackgrounded = true;
        _backgroundedAt = DateTime.UtcNow;
        
        // Save current state
        SaveAppState();
    }
    
    private void OnWindowActivated(object? sender, EventArgs e)
    {
        Console.WriteLine("Window: Activated");
        
        // Check if we're resuming from background
        if (_wasBackgrounded)
        {
            var backgroundDuration = DateTime.UtcNow - _backgroundedAt;
            Console.WriteLine($"Window: Resuming from background after {backgroundDuration.TotalSeconds:F1} seconds");
            
            // Restore state if needed
            RestoreAppState();
            
            // If backgrounded for a while, revalidate session
            if (backgroundDuration > SessionRevalidationThreshold)
            {
                Console.WriteLine("Window: Session revalidation threshold exceeded, will revalidate");
                _ = RevalidateSessionAsync();
            }
            
            _wasBackgrounded = false;
        }
    }
    
    private void OnWindowResumed(object? sender, EventArgs e)
    {
        Console.WriteLine("Window: Resumed from background");
        
        if (_wasBackgrounded)
        {
            var backgroundDuration = DateTime.UtcNow - _backgroundedAt;
            Console.WriteLine($"Window: Was backgrounded for {backgroundDuration.TotalSeconds:F1} seconds");
            
            // Restore state if needed
            RestoreAppState();
            
            // Always revalidate on Resume (this is a stronger signal than just Activated)
            if (Preferences.Get("IsLoggedIn", false))
            {
                Console.WriteLine("Window: User is logged in, revalidating session");
                _ = RevalidateSessionAsync();
            }
            
            _wasBackgrounded = false;
        }
    }
    
    /// <summary>
    /// Saves critical app state before going to background.
    /// Preferences are already persisted, but we ensure App.CurrentUser is synced.
    /// </summary>
    private void SaveAppState()
    {
        try
        {
            if (CurrentUser != null)
            {
                // Ensure user data is saved to preferences
                var userJson = JsonConvert.SerializeObject(CurrentUser);
                Preferences.Set("UserData", userJson);
                Console.WriteLine($"SaveAppState: Saved CurrentUser to preferences");
            }
            
            // Save current route for potential restoration
            if (Shell.Current?.CurrentState?.Location != null)
            {
                var currentRoute = Shell.Current.CurrentState.Location.ToString();
                Preferences.Set("LastRoute", currentRoute);
                Console.WriteLine($"SaveAppState: Saved current route: {currentRoute}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"SaveAppState: Error saving state: {ex.Message}");
        }
    }
    
    /// <summary>
    /// Restores app state when resuming from background.
    /// Particularly important if the OS tombstoned the app and we lost memory state.
    /// </summary>
    private void RestoreAppState()
    {
        try
        {
            // Check if CurrentUser was lost (tombstoning)
            if (CurrentUser == null && Preferences.Get("IsLoggedIn", false))
            {
                Console.WriteLine("RestoreAppState: CurrentUser is null but was logged in - restoring from preferences");
                
                var userJson = Preferences.Get("UserData", string.Empty);
                if (!string.IsNullOrEmpty(userJson))
                {
                    var user = JsonConvert.DeserializeObject<User>(userJson);
                    if (user != null)
                    {
                        CurrentUser = user;
                        Console.WriteLine($"RestoreAppState: Restored CurrentUser from preferences: {user.Email}");
                        
                        // Update the shell menu if needed
                        MainThread.BeginInvokeOnMainThread(() =>
                        {
                            if (Shell.Current?.BindingContext is AppShellViewModel vm)
                            {
                                vm.UpdateMenuItems();
                            }
                            if (Shell.Current is AppShell appShell)
                            {
                                appShell.UpdateMenuVisibility();
                            }
                        });
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"RestoreAppState: Error restoring state: {ex.Message}");
        }
    }
    
    /// <summary>
    /// Revalidates the user session with the server.
    /// Called when resuming from background to ensure session is still valid
    /// and to detect if user was logged out elsewhere.
    /// </summary>
    private async Task RevalidateSessionAsync()
    {
        try
        {
            if (!Preferences.Get("IsLoggedIn", false))
            {
                Console.WriteLine("RevalidateSessionAsync: Not logged in, skipping");
                return;
            }
            
            var authToken = Preferences.Get("AuthToken", null);
            if (string.IsNullOrEmpty(authToken))
            {
                Console.WriteLine("RevalidateSessionAsync: No auth token, skipping");
                return;
            }
            
            Console.WriteLine("RevalidateSessionAsync: Validating session with server...");
            
            var authService = Services.GetRequiredService<AuthService>();
            var (success, user, message) = await authService.ValidateSessionAsync();
            
            Console.WriteLine($"RevalidateSessionAsync: Result - Success={success}, HasUser={user != null}, Message={message}");
            
            if (success && user != null)
            {
                // Session is still valid - update user data
                CurrentUser = user;
                Console.WriteLine($"RevalidateSessionAsync: Session valid, updated CurrentUser");
                
                // Update menu
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    if (Shell.Current?.BindingContext is AppShellViewModel vm)
                    {
                        vm.UpdateMenuItems();
                    }
                });
            }
            else
            {
                // Session invalid - check if it's "logged in elsewhere"
                // The AuthService.ValidateSessionAsync will handle showing the popup and navigation
                // if it detects LOGGED_IN_ELSEWHERE
                string lowerMessage = message.ToLowerInvariant();
                bool isLoggedInElsewhere = message.StartsWith("LOGGED_IN_ELSEWHERE:", StringComparison.OrdinalIgnoreCase) ||
                                           lowerMessage.Contains("logged in elsewhere") ||
                                           lowerMessage.Contains("another device");
                
                if (isLoggedInElsewhere)
                {
                    // Already handled by AuthService
                    Console.WriteLine("RevalidateSessionAsync: User was logged in elsewhere, handled by AuthService");
                }
                else if (lowerMessage.Contains("network") || lowerMessage.Contains("connection") || lowerMessage.Contains("timeout"))
                {
                    // Network error - don't log user out, they might be offline temporarily
                    Console.WriteLine("RevalidateSessionAsync: Network error, keeping user logged in for now");
                }
                else
                {
                    // Session is truly invalid/expired
                    Console.WriteLine("RevalidateSessionAsync: Session invalid, clearing login state");
                    Preferences.Set("IsLoggedIn", false);
                    Preferences.Remove("AuthToken");
                    Preferences.Remove("UserData");
                    Preferences.Remove("CSRFToken");
                    CurrentUser = null;
                    
                    // Navigate to login
                    await MainThread.InvokeOnMainThreadAsync(async () =>
                    {
                        if (Shell.Current?.BindingContext is AppShellViewModel vm)
                        {
                            vm.UpdateMenuItems();
                        }
                        if (Shell.Current is AppShell appShell)
                        {
                            appShell.UpdateMenuVisibility();
                        }
                        await Shell.Current.GoToAsync("//Login");
                    });
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"RevalidateSessionAsync: Error: {ex.Message}");
            // Don't log user out on exception - might be temporary network issue
        }
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