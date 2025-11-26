using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HiatMeApp.Services;
using HiatMeApp.Models;
using System;
using System.Threading.Tasks;

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
        if (Preferences.Get("IsLoggedIn", false))
        {
            Email = Preferences.Get("UserEmail", string.Empty);
        }
        Console.WriteLine($"LoginViewModel: Initialized, IsLoggedIn={Preferences.Get("IsLoggedIn", false)}, Email={Email}");
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