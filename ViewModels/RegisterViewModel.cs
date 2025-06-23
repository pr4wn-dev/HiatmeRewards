using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HiatMeApp.Services;
using System;
using System.Threading.Tasks;

namespace HiatMeApp.ViewModels;

public partial class RegisterViewModel : BaseViewModel
{
    private readonly AuthService _authService;

    [ObservableProperty]
    private string? _name;

    [ObservableProperty]
    private string? _email;

    [ObservableProperty]
    private string? _phone;

    [ObservableProperty]
    private string? _password;

    [ObservableProperty]
    private string? _confirmPassword;

    [ObservableProperty]
    private string? _message;

    public RegisterViewModel(AuthService authService)
    {
        _authService = authService ?? throw new ArgumentNullException(nameof(authService));
        Title = "Register";
    }

    [RelayCommand]
    private async Task RegisterAsync()
    {
        if (IsBusy) return;

        if (Password != ConfirmPassword)
        {
            Message = "Passwords do not match.";
            return;
        }

        try
        {
            IsBusy = true;
            Message = "Registering...";

            var (success, message) = await _authService.RegisterAsync(Name, Email, Phone, Password);
            Message = message;

            if (success)
            {
                await Shell.Current.DisplayAlert("Success", "Registration successful. Check your email to verify.", "OK");
                await Shell.Current.GoToAsync("//Login");
            }
        }
        catch (Exception ex)
        {
            Message = $"Error: {ex.Message}";
            Console.WriteLine($"Register error: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task GoToLogin()
    {
        await Shell.Current.GoToAsync("//Login");
    }
}