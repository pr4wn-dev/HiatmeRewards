using HiatMeApp.ViewModels;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Storage;
using System.Threading.Tasks;
using Microsoft.Maui.ApplicationModel;

namespace HiatMeApp;

public partial class LoginPage : ContentPage
{
    public LoginPage(LoginViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
    
    protected override void OnAppearing()
    {
        base.OnAppearing();
        
        // Ensure email and password are loaded when page appears
        if (BindingContext is LoginViewModel vm)
        {
            // Always reload email from preferences to ensure it's displayed
            var savedEmail = Preferences.Get("SavedLoginEmail", string.Empty);
            vm.Email = savedEmail; // Set it even if empty to clear any stale data
            
            // Reload password if "Remember Me" was enabled
            var rememberMe = Preferences.Get("RememberLoginCredentials", true);
            if (rememberMe)
            {
                // Load password asynchronously
                Task.Run(async () =>
                {
                    try
                    {
                        var savedPassword = await SecureStorage.GetAsync("SavedLoginPassword");
                        MainThread.BeginInvokeOnMainThread(() =>
                        {
                            // Set password even if empty to clear any stale data
                            vm.Password = savedPassword ?? string.Empty;
                        });
                    }
                    catch
                    {
                        // SecureStorage might not be available, clear password
                        MainThread.BeginInvokeOnMainThread(() =>
                        {
                            vm.Password = string.Empty;
                        });
                    }
                });
            }
            else
            {
                // Clear password if remember me is disabled
                vm.Password = string.Empty;
            }
        }
    }
}