using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Storage;

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

    [ObservableProperty]
    private string _userRole = "User";

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
                        UserRole = user.Role ?? "User";
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
            UserRole = "Guest";
        }
        
        OnPropertyChanged(nameof(LoginMenuTitle));
        OnPropertyChanged(nameof(HomeMenuRoute));
        OnPropertyChanged(nameof(UserEmail));
        OnPropertyChanged(nameof(UserName));
        OnPropertyChanged(nameof(ProfilePicture));
        OnPropertyChanged(nameof(UserRole));
        Console.WriteLine($"UpdateMenuItems: IsLoggedIn={isLoggedIn}, LoginMenuTitle={LoginMenuTitle}, HomeMenuRoute={HomeMenuRoute}, UserEmail={UserEmail}");
        // Menu items are now managed programmatically in AppShell, so no need to update text here
    }

    [RelayCommand]
    private async Task LoginMenuCommand()
    {
        try
        {
            Console.WriteLine("LoginMenuCommand: Executing");
            if (Preferences.Get("IsLoggedIn", false))
            {
                // Logout - save email before clearing preferences
                string savedEmail = Preferences.Get("SavedLoginEmail", string.Empty);
                if (string.IsNullOrEmpty(savedEmail))
                {
                    // Fallback to current user email if SavedLoginEmail is not set
                    savedEmail = Preferences.Get("UserEmail", string.Empty);
                }
                
                Preferences.Clear();
                
                // Also clear saved password from SecureStorage
                try
                {
                    SecureStorage.Remove("SavedLoginPassword");
                }
                catch
                {
                    // SecureStorage might not be available, ignore
                }
                
                // Restore saved email after clearing (so it's remembered for next login)
                if (!string.IsNullOrEmpty(savedEmail))
                {
                    Preferences.Set("SavedLoginEmail", savedEmail);
                    Console.WriteLine($"Logout: Preserved email '{savedEmail}' after logout");
                }
                
                Console.WriteLine("Logout: Preferences and SecureStorage cleared (email preserved)");
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
    private async Task RequestDayOffMenuCommand()
    {
        try
        {
            await Shell.Current.GoToAsync("//RequestDayOff");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"RequestDayOffMenuCommand error: {ex.Message}");
        }
    }
}