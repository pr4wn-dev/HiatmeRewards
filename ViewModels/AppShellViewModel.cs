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

    [ObservableProperty]
    private string? _userName;

    [ObservableProperty]
    private string? _profilePicture;

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
        
        if (isLoggedIn)
        {
            UserEmail = Preferences.Get("UserEmail", "Not logged in");
            // Load user data from preferences
            var userJson = Preferences.Get("UserData", string.Empty);
            if (!string.IsNullOrEmpty(userJson))
            {
                try
                {
                    var user = Newtonsoft.Json.JsonConvert.DeserializeObject<Models.User>(userJson);
                    if (user != null)
                    {
                        UserName = user.Name;
                        ProfilePicture = user.ProfilePicture;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error deserializing user data: {ex.Message}");
                }
            }
        }
        else
        {
            UserEmail = "Not logged in";
            UserName = null;
            ProfilePicture = null;
        }
        
        OnPropertyChanged(nameof(LoginMenuTitle));
        OnPropertyChanged(nameof(HomeMenuRoute));
        OnPropertyChanged(nameof(UserEmail));
        OnPropertyChanged(nameof(UserName));
        OnPropertyChanged(nameof(ProfilePicture));
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

    [RelayCommand]
    private async Task ProfileMenuCommand()
    {
        try
        {
            await Shell.Current.GoToAsync("//Profile");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"ProfileMenuCommand error: {ex.Message}");
        }
    }

    [RelayCommand]
    private async Task ManageUsersMenuCommand()
    {
        // This would navigate to a web view or external page
        // For now, just log it
        Console.WriteLine("ManageUsersMenuCommand: Would open manage users page");
    }

    [RelayCommand]
    private async Task ManageVehiclesMenuCommand()
    {
        // This would navigate to a web view or external page
        // For now, just log it
        Console.WriteLine("ManageVehiclesMenuCommand: Would open manage vehicles page");
    }

    [RelayCommand]
    private async Task DayOffRequestsMenuCommand()
    {
        // This would navigate to a web view or external page
        // For now, just log it
        Console.WriteLine("DayOffRequestsMenuCommand: Would open day off requests page");
    }
}