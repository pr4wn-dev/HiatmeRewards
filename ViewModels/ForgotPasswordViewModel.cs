using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HiatMeApp.Services;
using System;
using System.Threading.Tasks;

namespace HiatMeApp.ViewModels;

public partial class ForgotPasswordViewModel : BaseViewModel
{
    private readonly AuthService _authService;

    [ObservableProperty]
    private string? _email;

    [ObservableProperty]
    private string? _message;

    public ForgotPasswordViewModel(AuthService authService)
    {
        _authService = authService ?? throw new ArgumentNullException(nameof(authService));
        Title = "Forgot Password";
    }

    [RelayCommand]
    private async Task SendResetAsync()
    {
        if (IsBusy) return;

        try
        {
            IsBusy = true;
            Message = "Sending...";

            var (success, message) = await _authService.ForgotPasswordAsync(Email);
            Message = message;

            if (success)
            {
                await Shell.Current.DisplayAlert("Success", "Reset link sent to your email.", "OK");
                await Shell.Current.GoToAsync("//Login");
            }
        }
        catch (Exception ex)
        {
            Message = $"Error: {ex.Message}";
            Console.WriteLine($"Forgot password error: {ex.Message}");
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