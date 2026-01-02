using HiatMeApp.ViewModels;
using HiatMeApp.Models;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Storage;
using Newtonsoft.Json;
using System.Collections.ObjectModel;
using System.IO;

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
            
            // Create ViewModel
            _viewModel = viewModel ?? (App.Services?.GetService<VehicleViewModel>() ?? new VehicleViewModel());
            
            // Ensure collections are initialized
            if (_viewModel.Vehicles == null)
            {
                _viewModel.Vehicles = new ObservableCollection<Vehicle>();
            }
            
            // Set BindingContext
            BindingContext = _viewModel;
        }
        catch (Exception ex)
        {
            LogError($"VehiclePage: Constructor CRASH: {ex.Message}\n{ex.StackTrace}");
            try
            {
                _viewModel = new VehicleViewModel();
                if (_viewModel.Vehicles == null)
                {
                    _viewModel.Vehicles = new ObservableCollection<Vehicle>();
                }
                BindingContext = _viewModel;
            }
            catch { }
        }
    }
    
    private void LogError(string message)
    {
        try
        {
            // Try multiple locations to ensure we can write
            string logPath = null;
            
            // Try cache directory first (more accessible)
            try
            {
                logPath = Path.Combine(FileSystem.CacheDirectory, "vehicle_page_log.txt");
            }
            catch
            {
                // Fallback to app data directory
                try
                {
                    logPath = Path.Combine(FileSystem.AppDataDirectory, "vehicle_page_log.txt");
                }
                catch
                {
                    // Last resort - try temp path
                    logPath = Path.Combine(Path.GetTempPath(), "vehicle_page_log.txt");
                }
            }
            
            var logEntry = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}\n";
            File.AppendAllText(logPath, logEntry);
            System.Diagnostics.Debug.WriteLine($"LOG: {message}");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"LOG ERROR: {ex.Message}");
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
            LogError($"VehiclePage: base.OnAppearing error: {ex.Message}");
        }
        
        try
        {
            await Task.Delay(300);
            
            // Restore user
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
            
            // Load vehicles
            if (BindingContext is VehicleViewModel vm)
            {
                try
                {
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
                    LogError($"VehiclePage: LoadVehiclesAsync error: {ex.Message}\n{ex.StackTrace}");
                }
            }
        }
        catch (Exception ex)
        {
            LogError($"VehiclePage: OnAppearing error: {ex.Message}\n{ex.StackTrace}");
        }
    }
}