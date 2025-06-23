using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HiatMeApp.Services;
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
    }

    [RelayCommand]
    private async Task LoginAsync()
    {
        if (IsBusy) return;

        try
        {
            IsBusy = true;
            Message = "Logging in...";

            var (success, user, message) = await _authService.LoginAsync(Email, Password);
            Message = message;

            if (success && user != null)
            {
                Preferences.Set("IsLoggedIn", true);
                Preferences.Set("UserEmail", user.Email ?? string.Empty);
                await Shell.Current.GoToAsync("//Home");
            }
        }
        catch (Exception ex)
        {
            Message = $"Error: {ex.Message}";
            Console.WriteLine($"Login error: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task GoToRegister()
    {
        await Shell.Current.GoToAsync("Register");
    }

    [RelayCommand]
    private async Task GoToForgotPassword()
    {
        await Shell.Current.GoToAsync("ForgotPassword");
    }
}