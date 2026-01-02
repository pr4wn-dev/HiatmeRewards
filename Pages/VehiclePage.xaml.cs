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
        
        // Restore user data BEFORE setting binding context to prevent crashes
        if (App.CurrentUser == null)
        {
            Console.WriteLine("VehiclePage: App.CurrentUser is null in constructor, attempting to restore from stored data");
            var userDataJson = Preferences.Get("UserData", string.Empty);
            if (!string.IsNullOrEmpty(userDataJson))
            {
                try
                {
                    var storedUser = JsonConvert.DeserializeObject<Models.User>(userDataJson);
                    if (storedUser != null)
                    {
                        App.CurrentUser = storedUser;
                        Console.WriteLine($"VehiclePage: Restored user from stored data in constructor, Email={storedUser.Email}, Role={storedUser.Role}");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"VehiclePage: Failed to restore user data in constructor: {ex.Message}");
                }
            }
        }
        
        // Use the passed-in viewModel instead of creating a new one
        BindingContext = viewModel ?? new VehicleViewModel();
        Console.WriteLine("VehiclePage: BindingContext set to VehicleViewModel");
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        
        // Ensure user data is restored if it's missing (might happen if app was closed and reopened)
        if (App.CurrentUser == null)
        {
            Console.WriteLine("VehiclePage: App.CurrentUser is null in OnAppearing, attempting to restore from stored data");
            var userDataJson = Preferences.Get("UserData", string.Empty);
            if (!string.IsNullOrEmpty(userDataJson))
            {
                try
                {
                    var storedUser = JsonConvert.DeserializeObject<Models.User>(userDataJson);
                    if (storedUser != null)
                    {
                        App.CurrentUser = storedUser;
                        Console.WriteLine($"VehiclePage: Restored user from stored data in OnAppearing, Email={storedUser.Email}, Role={storedUser.Role}");
                        
                        // Refresh the view model with the restored user data
                        if (BindingContext is VehicleViewModel vm)
                        {
                            vm.LoadVehicles();
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"VehiclePage: Failed to restore user data in OnAppearing: {ex.Message}");
                }
            }
        }
        else if (BindingContext is VehicleViewModel vm)
        {
            // User is already set, just refresh the vehicles
            vm.LoadVehicles();
        }
    }
}