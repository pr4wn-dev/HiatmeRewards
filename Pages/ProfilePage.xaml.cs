using HiatMeApp.ViewModels;
using Microsoft.Maui.Controls;

namespace HiatMeApp;

public partial class ProfilePage : ContentPage
{
    public ProfilePage(ProfileViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}

