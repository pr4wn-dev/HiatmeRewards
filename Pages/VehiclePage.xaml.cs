using HiatMeApp.ViewModels;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Storage;
using Newtonsoft.Json;

namespace HiatMeApp;

public partial class VehiclePage : ContentPage
{
    // Default constructor for DataTemplate (used by AppShell)
    public VehiclePage() : this(GetViewModel())
    {
    }
    
    private static VehicleViewModel GetViewModel()
    {
        try
        {
            // Try to get from DI, fallback to new instance if not available
            if (App.Services != null)
            {
                return App.Services.GetService<VehicleViewModel>() ?? new VehicleViewModel();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"VehiclePage: Error getting ViewModel from DI: {ex.Message}");
        }
        return new VehicleViewModel();
    }

    // Constructor with dependency injection
    public VehiclePage(VehicleViewModel viewModel)
    {
        try
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
                            Console.WriteLine($"VehiclePage: Restored user from stored data in constructor, Email={storedUser.Email}, Role={storedUser.Role}, VehiclesCount={storedUser.Vehicles?.Count ?? 0}");
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"VehiclePage: Failed to restore user data in constructor: {ex.Message}, StackTrace: {ex.StackTrace}");
                    }
                }
            }
            
            // Use the passed-in viewModel instead of creating a new one
            // If viewModel is null, create a new one (defensive)
            if (viewModel == null)
            {
                Console.WriteLine("VehiclePage: viewModel parameter is null, creating new VehicleViewModel");
                viewModel = new VehicleViewModel();
            }
            
            BindingContext = viewModel;
            Console.WriteLine($"VehiclePage: BindingContext set to VehicleViewModel, HasVehicle={viewModel.Vehicle != null}");
            
            // Set NavigationBar BindingContext immediately after setting page BindingContext
            // Use a small delay to ensure Content is loaded
            Microsoft.Maui.ApplicationModel.MainThread.BeginInvokeOnMainThread(async () =>
            {
                await Task.Delay(50);
                SetNavigationBarBindingContext(viewModel);
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"VehiclePage: Exception in constructor: {ex.Message}, StackTrace: {ex.StackTrace}");
            // Try to create a basic view model even if there's an error
            try
            {
                var fallbackViewModel = new VehicleViewModel();
                BindingContext = fallbackViewModel;
                Microsoft.Maui.ApplicationModel.MainThread.BeginInvokeOnMainThread(async () =>
                {
                    await Task.Delay(50);
                    SetNavigationBarBindingContext(fallbackViewModel);
                });
            }
            catch
            {
                // If even that fails, we're in trouble but at least we logged it
            }
        }
    }
    
    private void SetNavigationBarBindingContext(VehicleViewModel vm)
    {
        try
        {
            if (Content is Grid grid)
            {
                foreach (var child in grid.Children)
                {
                    if (child is Controls.NavigationBar navBar)
                    {
                        navBar.BindingContext = vm;
                        Console.WriteLine("VehiclePage: NavigationBar BindingContext set in SetNavigationBarBindingContext");
                        return;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"VehiclePage: Error setting NavigationBar BindingContext: {ex.Message}");
        }
    }

    protected override async void OnAppearing()
    {
        try
        {
            base.OnAppearing();
            
            // Small delay to ensure page is fully loaded
            await Task.Delay(100);
            
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
                            Console.WriteLine($"VehiclePage: Restored user from stored data in OnAppearing, Email={storedUser.Email}, Role={storedUser.Role}, VehiclesCount={storedUser.Vehicles?.Count ?? 0}");
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"VehiclePage: Failed to restore user data in OnAppearing: {ex.Message}, StackTrace: {ex.StackTrace}");
                    }
                }
            }
            
            // Always try to load vehicles, regardless of whether user was just restored
            if (BindingContext is VehicleViewModel vm)
            {
                try
                {
                    Console.WriteLine($"VehiclePage: Calling LoadVehicles, App.CurrentUser={(App.CurrentUser != null ? $"Email={App.CurrentUser.Email}, VehiclesCount={App.CurrentUser.Vehicles?.Count ?? 0}" : "null")}");
                    vm.LoadVehicles();
                    Console.WriteLine($"VehiclePage: LoadVehicles completed, Vehicle={(vm.Vehicle != null ? $"VIN ending {vm.Vehicle.LastSixVin}" : "null")}, NoVehicleMessageVisible={vm.NoVehicleMessageVisible}");
                    
                    // Ensure NavigationBar visibility properties are set
                    if (App.CurrentUser != null)
                    {
                        vm.IsVehicleButtonVisible = App.CurrentUser.Role is "Driver" or "Manager" or "Owner";
                        vm.IsIssuesButtonVisible = App.CurrentUser.Role is "Driver" or "Manager" or "Owner";
                    }
                    
                    // Set NavigationBar BindingContext explicitly (ContentView doesn't inherit automatically)
                    SetNavigationBarBindingContext(vm);
                    
                    // Force UI update
                    OnPropertyChanged(nameof(BindingContext));
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"VehiclePage: Error calling LoadVehicles in OnAppearing: {ex.Message}, StackTrace: {ex.StackTrace}");
                }
            }
            else
            {
                Console.WriteLine($"VehiclePage: BindingContext is not VehicleViewModel, type={BindingContext?.GetType()?.Name ?? "null"}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"VehiclePage: Exception in OnAppearing: {ex.Message}, StackTrace: {ex.StackTrace}");
        }
    }
}