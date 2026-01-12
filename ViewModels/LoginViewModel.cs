using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HiatMeApp.Services;
using HiatMeApp.Models;
using System;
using System.Threading.Tasks;
using Microsoft.Maui.Storage;

namespace HiatMeApp.ViewModels;

public partial class LoginViewModel : BaseViewModel
{
    private readonly AuthService _authService;

    [ObservableProperty]
    private string? _email;

    [ObservableProperty]
    private string? _password;

    [ObservableProperty]
    private string? _message;

    public LoginViewModel(AuthService authService)
    {
        _authService = authService ?? throw new ArgumentNullException(nameof(authService));
        Title = "Login";
        
        // Always load saved email (username) - this is always remembered
        LoadSavedEmail();
        
        // Never load password - it's always cleared for security
        Password = string.Empty;
        
        Console.WriteLine($"LoginViewModel: Initialized, Email={Email ?? "(empty)"}");
    }
    
    private void LoadSavedEmail()
    {
        // Always load saved email if available
        var savedEmail = Preferences.Get("SavedLoginEmail", string.Empty);
        if (!string.IsNullOrEmpty(savedEmail))
        {
            Email = savedEmail;
        }
        else if (Preferences.Get("IsLoggedIn", false))
        {
            // Fallback to current logged-in email if no saved email
            Email = Preferences.Get("UserEmail", string.Empty);
        }
    }

    [RelayCommand]
    private async Task LoginAsync()
    {
        if (IsBusy) return;

        try
        {
            IsBusy = true;
            Message = "Logging in...";
            Console.WriteLine($"LoginAsync: Attempting login with Email={Email}");

            var (success, user, message) = await _authService.LoginAsync(Email, Password);
            Message = message;

            if (success && user != null)
            {
                // Store user in App for global access
                App.CurrentUser = user;
                Preferences.Set("IsLoggedIn", true);
                Preferences.Set("UserEmail", user.Email ?? string.Empty);
                Preferences.Set("UserRole", user.Role ?? string.Empty);
                Preferences.Set("UserData", Newtonsoft.Json.JsonConvert.SerializeObject(user)); // Ensure vehicles are stored
                Preferences.Set("ShouldConfirmVehicle", true); // Flag to show vehicle confirmation after login
                
                // Always save email for auto-fill (username is always remembered)
                Preferences.Set("SavedLoginEmail", Email ?? string.Empty);
                
                // Never save password - always clear it for security
                try
                {
                    SecureStorage.Remove("SavedLoginPassword");
                }
                catch { }
                
                Console.WriteLine($"LoginAsync: Success, Email={user.Email}, Role={user.Role}, VehiclesCount={user.Vehicles?.Count ?? 0}");

                // Navigate to Home for all roles
                string route = "//Home";
                Console.WriteLine($"LoginAsync: Navigating to {route}");
                await Shell.Current.GoToAsync(route);

                if (Shell.Current.BindingContext is AppShellViewModel shellViewModel)
                {
                    shellViewModel.UpdateMenuItems();
                    Console.WriteLine("LoginAsync: Menu items updated");
                }
                
                // Update menu visibility in AppShell (on main thread)
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    if (Shell.Current is AppShell appShell)
                    {
                        appShell.UpdateMenuVisibility();
                        Console.WriteLine("LoginAsync: Menu visibility updated");
                    }
                });
                
                // Start location tracking for eligible roles (Driver, Manager, Owner)
                await App.StartLocationTrackingAsync();
                
                // Save OneSignal player ID for push notifications (Android only)
#if ANDROID
                _ = Task.Run(async () =>
                {
                    try
                    {
                        // Wait for OneSignal to initialize and try multiple times
                        string? playerId = null;
                        
                        // Try up to 5 times with increasing delays
                        for (int attempt = 1; attempt <= 5; attempt++)
                        {
                            await Task.Delay(attempt * 1000); // 1s, 2s, 3s, 4s, 5s
                            
                            playerId = HiatmeApp.MainApplication.GetOneSignalPlayerId();
                            Console.WriteLine($"LoginAsync: OneSignal attempt {attempt}, Player ID: {playerId ?? "null"}");
                            
                            if (!string.IsNullOrEmpty(playerId))
                            {
                                break;
                            }
                        }
                        
                        if (!string.IsNullOrEmpty(playerId))
                        {
                            Console.WriteLine($"LoginAsync: Saving OneSignal player ID: {playerId}");
                            var (saveSuccess, saveMessage) = await _authService.SaveOneSignalPlayerIdAsync(playerId);
                            Console.WriteLine($"LoginAsync: Save player ID result: {saveSuccess}, {saveMessage}");
                        }
                        else
                        {
                            Console.WriteLine("LoginAsync: OneSignal player ID not available after 5 attempts");
                            // Request notification permission - the subscription observer will save when ready
                            HiatmeApp.MainApplication.RequestNotificationPermission();
                        }
                    }
                    catch (Exception osEx)
                    {
                        Console.WriteLine($"LoginAsync: OneSignal error: {osEx.Message}");
                    }
                });
#endif
            }
        }
        catch (Exception ex)
        {
            Message = $"Error: {ex.Message}";
            Console.WriteLine($"LoginAsync error: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task GoToRegister()
    {
        Console.WriteLine("GoToRegister: Navigating to Register");
        await Shell.Current.GoToAsync("Register");
    }

    [RelayCommand]
    private async Task GoToForgotPassword()
    {
        Console.WriteLine("GoToForgotPassword: Navigating to ForgotPassword");
        await Shell.Current.GoToAsync("ForgotPassword");
    }
}