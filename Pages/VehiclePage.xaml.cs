using HiatMeApp.ViewModels;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Storage;
using Newtonsoft.Json;

namespace HiatMeApp;

public partial class VehiclePage : ContentPage
{
    private VehicleViewModel? _viewModel;

    // Default constructor for DataTemplate (used by AppShell)
    public VehiclePage() : this(null)
    {
    }

    // Constructor with dependency injection
    public VehiclePage(VehicleViewModel? viewModel)
    {
        try
        {
            InitializeComponent();
            
            // Restore user if needed
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
                        }
                    }
                }
                catch { }
            }
            
            // Create or use provided ViewModel
            _viewModel = viewModel ?? (App.Services?.GetService<VehicleViewModel>() ?? new VehicleViewModel());
            BindingContext = _viewModel;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"VehiclePage: Constructor error: {ex.Message}");
            try
            {
                _viewModel = new VehicleViewModel();
                BindingContext = _viewModel;
            }
            catch { }
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
                    await Task.Delay(100);
                    if (Content is Grid grid)
                    {
                        foreach (var child in grid.Children)
                        {
                            if (child is Controls.NavigationBar navBar)
                            {
                                navBar.BindingContext = vm;
                                break;
                            }
                        }
                    }
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