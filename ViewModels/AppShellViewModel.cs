using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;

namespace HiatMeApp.ViewModels;

public partial class AppShellViewModel : ObservableObject
{
    [ObservableProperty]
    private string _loginMenuTitle = "Login"; // Explicit default

    [ObservableProperty]
    private string _homeMenuRoute = "Login"; // Explicit default

    [ObservableProperty]
    private string _userEmail = "Not logged in"; // For flyout header

    public AppShellViewModel()
    {
        UpdateMenuItems();
        Console.WriteLine($"AppShellViewModel constructor: IsLoggedIn={Preferences.Get("IsLoggedIn", false)}, LoginMenuTitle={LoginMenuTitle}, HomeMenuRoute={HomeMenuRoute}, UserEmail={UserEmail}");
    }

    public void UpdateMenuItems()
    {
        bool isLoggedIn = Preferences.Get("IsLoggedIn", false);
        LoginMenuTitle = isLoggedIn ? "Logout" : "Login";
        HomeMenuRoute = isLoggedIn ? "Home" : "Login";
        UserEmail = isLoggedIn ? Preferences.Get("UserEmail", "Not logged in") : "Not logged in";
        OnPropertyChanged(nameof(LoginMenuTitle));
        OnPropertyChanged(nameof(HomeMenuRoute));
        OnPropertyChanged(nameof(UserEmail));
        Console.WriteLine($"UpdateMenuItems: IsLoggedIn={isLoggedIn}, LoginMenuTitle={LoginMenuTitle}, HomeMenuRoute={HomeMenuRoute}, UserEmail={UserEmail}");
        // Force UI refresh
        MainThread.BeginInvokeOnMainThread(() =>
        {
            if (Shell.Current is AppShell shell && shell.FindByName("LoginMenuItem") is MenuItem loginMenuItem)
            {
                loginMenuItem.Text = LoginMenuTitle;
                Console.WriteLine($"UpdateMenuItems: Forced LoginMenuItem.Text={LoginMenuTitle}");
            }
        });
    }

    [RelayCommand]
    private async Task LoginMenuCommand()
    {
        try
        {
            Console.WriteLine("LoginMenuCommand: Executing");
            if (Preferences.Get("IsLoggedIn", false))
            {
                Preferences.Clear();
                Console.WriteLine("Logout: Preferences cleared");
                LoginMenuTitle = "Login";
                UserEmail = "Not logged in";
                OnPropertyChanged(nameof(LoginMenuTitle));
                OnPropertyChanged(nameof(UserEmail));
            }
            await Shell.Current.GoToAsync("//Login");
            UpdateMenuItems();
            Console.WriteLine("LoginMenuCommand: Navigated to Login");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"LoginMenuCommand error: {ex.Message}");
        }
    }

    [RelayCommand]
    private async Task HomeMenuCommand()
    {
        try
        {
            Console.WriteLine("HomeMenuCommand: Executing");
            Console.WriteLine($"HomeMenuCommand: Navigating to //{HomeMenuRoute}");
            await Shell.Current.GoToAsync($"//{HomeMenuRoute}");
            Console.WriteLine($"HomeMenuCommand: Navigated to {HomeMenuRoute}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"HomeMenuCommand error: {ex.Message}");
        }
    }
}