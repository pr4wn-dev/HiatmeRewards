using HiatMeApp.ViewModels;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Storage;
using System.Threading.Tasks;

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
            // Reload email to ensure it's displayed (always set if saved email exists)
            var savedEmail = Preferences.Get("SavedLoginEmail", string.Empty);
            if (!string.IsNullOrEmpty(savedEmail))
            {
                vm.Email = savedEmail;
            }
            
            // Reload password if "Remember Me" was enabled
            var rememberMe = Preferences.Get("RememberLoginCredentials", true);
            if (rememberMe && string.IsNullOrEmpty(vm.Password))
            {
                // Load password asynchronously
                Task.Run(async () =>
                {
                    try
                    {
                        var savedPassword = await SecureStorage.GetAsync("SavedLoginPassword");
                        if (!string.IsNullOrEmpty(savedPassword))
                        {
                            MainThread.BeginInvokeOnMainThread(() =>
                            {
                                vm.Password = savedPassword;
                            });
                        }
                    }
                    catch
                    {
                        // SecureStorage might not be available, ignore
                    }
                });
            }
        }
    }
}