using HiatMeApp.ViewModels;
using Microsoft.Maui.Controls;

namespace HiatMeApp;

public partial class VehicleIssuesPage : ContentPage
{
    private readonly VehicleIssuesViewModel _viewModel;

    public VehicleIssuesPage(VehicleIssuesViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (App.CurrentUser?.Vehicles?.Any(v => v.CurrentUserId == App.CurrentUser.UserId) == true)
        {
            var vehicleId = App.CurrentUser.Vehicles.First(v => v.CurrentUserId == App.CurrentUser.UserId).VehicleId;
            _viewModel.Initialize(vehicleId);
            await _viewModel.LoadIssuesAsync(); // Call directly
        }
        else
        {
            Console.WriteLine("VehicleIssuesPage: No assigned vehicle found.");
            await DisplayAlert("Error", "No vehicle assigned. Please assign a vehicle first.", "OK");
            await Shell.Current.GoToAsync("//Vehicle");
        }
    }
}