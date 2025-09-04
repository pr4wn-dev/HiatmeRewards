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

    [ObservableProperty]
    private bool _isBusy; // Added to ensure UI notification

    public RegisterViewModel(AuthService authService)
    {
        _authService = authService ?? throw new ArgumentNullException(nameof(authService));
        Title = "Register";
        Console.WriteLine("RegisterViewModel initialized.");
    }

    [RelayCommand]
    private async Task RegisterAsync()
    {
        Console.WriteLine("RegisterAsync command started.");
        if (IsBusy)
        {
            Console.WriteLine("RegisterAsync skipped: IsBusy is true.");
            Message = "Operation in progress. Please wait.";
            return;
        }

        if (string.IsNullOrEmpty(Name) || string.IsNullOrEmpty(Email) || string.IsNullOrEmpty(Password) || string.IsNullOrEmpty(ConfirmPassword))
        {
            Message = "Please fill all required fields.";
            Console.WriteLine("RegisterAsync failed: Missing required fields.");
            return;
        }

        if (Password != ConfirmPassword)
        {
            Message = "Passwords do not match.";
            Console.WriteLine("RegisterAsync failed: Passwords do not match.");
            return;
        }

        try
        {
            IsBusy = true;
            Message = "Registering...";
            Console.WriteLine($"RegisterAsync: Attempting registration for Email={Email}");

            var (success, message) = await _authService.RegisterAsync(Name, Email, Phone, Password);
            Message = message; // Ensure UI updates with backend message
            Console.WriteLine($"RegisterAsync: AuthService returned Success={success}, Message={message}");

            if (success)
            {
                Console.WriteLine("RegisterAsync: Registration successful, navigating to Login.");
                await Shell.Current.DisplayAlert("Success", "Registration successful. Check your email to verify.", "OK");
                await Shell.Current.GoToAsync("//Login");
            }
        }
        catch (Exception ex)
        {
            Message = $"Error: {ex.Message}";
            Console.WriteLine($"RegisterAsync error: {ex.Message}, StackTrace: {ex.StackTrace}");
        }
        finally
        {
            IsBusy = false;
            Console.WriteLine("RegisterAsync command completed.");
        }
    }

    [RelayCommand]
    private async Task GoToLogin()
    {
        Console.WriteLine("GoToLogin command executed.");
        await Shell.Current.GoToAsync("//Login");
    }
}