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
        }
        catch (Exception ex)
        {
            Console.WriteLine($"VehiclePage: CRITICAL - InitializeComponent failed: {ex.Message}, StackTrace: {ex.StackTrace}");
            // Continue anyway - page might still work
        }
        
        try
        {
            // Restore user data BEFORE setting binding context to prevent crashes
            if (App.CurrentUser == null)
            {
                try
                {
                    var userDataJson = Preferences.Get("UserData", string.Empty);
                    if (!string.IsNullOrEmpty(userDataJson))
                    {
                        var storedUser = JsonConvert.DeserializeObject<Models.User>(userDataJson);
                        if (storedUser != null)
                        {
                            App.CurrentUser = storedUser;
                            Console.WriteLine($"VehiclePage: Restored user, Email={storedUser.Email}, VehiclesCount={storedUser.Vehicles?.Count ?? 0}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"VehiclePage: Failed to restore user: {ex.Message}");
                }
            }
            
            // Ensure viewModel is not null
            if (viewModel == null)
            {
                Console.WriteLine("VehiclePage: viewModel is null, creating new one");
                viewModel = new VehicleViewModel();
            }
            
            BindingContext = viewModel;
            Console.WriteLine($"VehiclePage: BindingContext set");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"VehiclePage: CRITICAL - Constructor error: {ex.Message}, StackTrace: {ex.StackTrace}");
            // Create fallback ViewModel
            try
            {
                BindingContext = new VehicleViewModel();
            }
            catch
            {
                Console.WriteLine("VehiclePage: CRITICAL - Could not create fallback ViewModel");
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
        }
        catch (Exception ex)
        {
            Console.WriteLine($"VehiclePage: Error in base.OnAppearing: {ex.Message}");
        }
        
        try
        {
            // Small delay to ensure page is fully loaded
            await Task.Delay(200);
            
            // Restore user if missing
            if (App.CurrentUser == null)
            {
                try
                {
                    var userDataJson = Preferences.Get("UserData", string.Empty);
                    if (!string.IsNullOrEmpty(userDataJson))
                    {
                        var storedUser = JsonConvert.DeserializeObject<Models.User>(userDataJson);
                        if (storedUser != null)
                        {
                            App.CurrentUser = storedUser;
                            Console.WriteLine($"VehiclePage: Restored user in OnAppearing, VehiclesCount={storedUser.Vehicles?.Count ?? 0}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"VehiclePage: Failed to restore user in OnAppearing: {ex.Message}");
                }
            }
            
            // Load vehicles if ViewModel exists
            if (BindingContext is VehicleViewModel vm)
            {
                try
                {
                    // Wait for vehicles to load before setting up UI
                    await vm.LoadVehiclesAsync();
                    
                    // Set NavigationBar BindingContext after data is loaded
                    await Task.Delay(150);
                    SetNavigationBarBindingContext(vm);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"VehiclePage: CRITICAL - Error in LoadVehiclesAsync: {ex.Message}, StackTrace: {ex.StackTrace}");
                }
            }
            else
            {
                Console.WriteLine($"VehiclePage: BindingContext is not VehicleViewModel: {BindingContext?.GetType()?.Name ?? "null"}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"VehiclePage: CRITICAL - Exception in OnAppearing: {ex.Message}, StackTrace: {ex.StackTrace}");
        }
    }
}