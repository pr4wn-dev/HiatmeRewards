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
        
        // Ensure email is loaded when page appears (password is never saved)
        if (BindingContext is LoginViewModel vm)
        {
            // Always reload email from preferences to ensure it's displayed
            var savedEmail = Preferences.Get("SavedLoginEmail", string.Empty);
            vm.Email = savedEmail; // Set it even if empty to clear any stale data
            
            // Always clear password - never remember it
            vm.Password = string.Empty;
        }
    }
}