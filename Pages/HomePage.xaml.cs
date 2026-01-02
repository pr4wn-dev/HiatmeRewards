using HiatMeApp.ViewModels;
using Microsoft.Maui.Controls;
using System.IO;

namespace HiatMeApp;

public partial class HomePage : ContentPage
{
    public HomePage(HomeViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
    
    private async void OnViewLogClicked(object sender, EventArgs e)
    {
        try
        {
            await Shell.Current.GoToAsync("//ViewLog");
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Could not open log page: {ex.Message}", "OK");
        }
    }
}