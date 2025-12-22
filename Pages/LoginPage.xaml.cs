using HiatMeApp.ViewModels;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Storage;

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
        
        // Ensure email is loaded when page appears (in case it wasn't set before binding)
        if (BindingContext is LoginViewModel vm)
        {
            // Reload email to ensure it's displayed (always set if saved email exists)
            var savedEmail = Preferences.Get("SavedLoginEmail", string.Empty);
            if (!string.IsNullOrEmpty(savedEmail))
            {
                vm.Email = savedEmail;
            }
        }
    }
}