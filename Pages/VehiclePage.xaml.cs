using HiatMeApp.ViewModels;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Storage;
using Newtonsoft.Json;

namespace HiatMeApp;

public partial class VehiclePage : ContentPage
{
    public VehiclePage(VehicleViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = new VehicleViewModel();
        Console.WriteLine("VehiclePage: BindingContext set to VehicleViewModel");
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        
        // Ensure user data is restored if it's missing (might happen if app was closed and reopened)
        if (App.CurrentUser == null)
        {
            Console.WriteLine("VehiclePage: App.CurrentUser is null, attempting to restore from stored data");
            var userDataJson = Preferences.Get("UserData", string.Empty);
            if (!string.IsNullOrEmpty(userDataJson))
            {
                try
                {
                    var storedUser = JsonConvert.DeserializeObject<Models.User>(userDataJson);
                    if (storedUser != null)
                    {
                        App.CurrentUser = storedUser;
                        Console.WriteLine($"VehiclePage: Restored user from stored data, Email={storedUser.Email}, Role={storedUser.Role}");
                        
                        // Refresh the view model with the restored user data
                        if (BindingContext is VehicleViewModel vm)
                        {
                            vm.LoadVehicles();
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"VehiclePage: Failed to restore user data: {ex.Message}");
                }
            }
        }
    }
}