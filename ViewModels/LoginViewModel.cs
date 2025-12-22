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
        
        // Load saved email synchronously first (so it appears immediately)
        LoadSavedEmail();
        
        // Then load password asynchronously
        LoadSavedPassword();
        
        Console.WriteLine($"LoginViewModel: Initialized, Email={Email ?? "(empty)"}, HasPassword={!string.IsNullOrEmpty(Password)}");
    }
    
    private void LoadSavedEmail()
    {
        // Load saved email (always load if available)
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
    
    private async void LoadSavedPassword()
    {
        // Load saved password if "Remember Me" was enabled
        var rememberMe = Preferences.Get("RememberLoginCredentials", true); // Default to true
        if (rememberMe)
        {
            // Use SecureStorage for password
            try
            {
                var savedPassword = await SecureStorage.GetAsync("SavedLoginPassword");
                if (!string.IsNullOrEmpty(savedPassword))
                {
                    Password = savedPassword;
                }
            }
            catch
            {
                // SecureStorage might not be available, ignore
            }
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
                
                // Save login credentials for auto-fill (always save email, password if remember me is enabled)
                Preferences.Set("SavedLoginEmail", Email ?? string.Empty);
                var rememberMe = Preferences.Get("RememberLoginCredentials", true); // Default to true
                if (rememberMe && !string.IsNullOrEmpty(Password))
                {
                    try
                    {
                        await SecureStorage.SetAsync("SavedLoginPassword", Password);
                    }
                    catch
                    {
                        // SecureStorage might not be available, ignore
                    }
                }
                else
                {
                    // Clear saved password if remember me is disabled
                    try
                    {
                        SecureStorage.Remove("SavedLoginPassword");
                    }
                    catch { }
                }
                
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