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
        LogError("VehiclePage: Constructor START");
        try
        {
            LogError("VehiclePage: About to InitializeComponent");
            InitializeComponent();
            LogError("VehiclePage: InitializeComponent SUCCESS");
            
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
                            LogError($"VehiclePage: Restored user, VehiclesCount={storedUser.Vehicles?.Count ?? 0}");
                        }
                    }
                }
                catch (Exception ex) { LogError($"VehiclePage: Restore user error: {ex.Message}"); }
            }
            
            // Create or use provided ViewModel
            LogError("VehiclePage: Creating ViewModel");
            _viewModel = viewModel ?? (App.Services?.GetService<VehicleViewModel>() ?? new VehicleViewModel());
            LogError("VehiclePage: ViewModel created");
            
            // Ensure all collections are initialized
            if (_viewModel.Vehicles == null)
            {
                _viewModel.Vehicles = new ObservableCollection<Vehicle>();
            }
            
            // Set BindingContext AFTER everything is initialized
            LogError("VehiclePage: Setting BindingContext");
            BindingContext = _viewModel;
            LogError("VehiclePage: BindingContext SET - Constructor SUCCESS");
        }
        catch (Exception ex)
        {
            LogError($"VehiclePage: Constructor CRASH: {ex.Message}\n{ex.StackTrace}");
            try
            {
                _viewModel = new VehicleViewModel();
                BindingContext = _viewModel;
            }
            catch (Exception ex2) { LogError($"VehiclePage: Fallback ViewModel failed: {ex2.Message}"); }
        }
    }
    
    private void LogError(string message)
    {
        try
        {
            var logPath = Path.Combine(FileSystem.AppDataDirectory, "vehicle_page_log.txt");
            var logEntry = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}\n";
            File.AppendAllText(logPath, logEntry);
            // Also write to console for Visual Studio debugging
            System.Diagnostics.Debug.WriteLine(message);
        }
        catch { }
    }
    
    // Method to get log file path for display
    public static string GetLogPath()
    {
        return Path.Combine(FileSystem.AppDataDirectory, "vehicle_page_log.txt");
    }
    
    // Method to read log content
    public static string ReadLog()
    {
        try
        {
            var logPath = GetLogPath();
            if (File.Exists(logPath))
            {
                return File.ReadAllText(logPath);
            }
            return "Log file not found.";
        }
        catch (Exception ex)
        {
            return $"Error reading log: {ex.Message}";
        }
    }
    

    protected override async void OnAppearing()
    {
        LogError("VehiclePage: OnAppearing START");
        try
        {
            base.OnAppearing();
            LogError("VehiclePage: base.OnAppearing SUCCESS");
        }
        catch (Exception ex)
        {
            LogError($"VehiclePage: base.OnAppearing ERROR: {ex.Message}");
        }
        
        try
        {
            await Task.Delay(200);
            LogError("VehiclePage: Delay complete");
            
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
                            LogError($"VehiclePage: Restored user, VehiclesCount={storedUser.Vehicles?.Count ?? 0}");
                        }
                    }
                }
                catch (Exception ex) { LogError($"VehiclePage: Restore user error: {ex.Message}"); }
            }
            
            if (BindingContext is VehicleViewModel vm)
            {
                LogError("VehiclePage: BindingContext is VehicleViewModel, calling LoadVehiclesAsync");
                try
                {
                    await vm.LoadVehiclesAsync();
                    LogError("VehiclePage: LoadVehiclesAsync SUCCESS");
                }
                catch (Exception ex)
                {
                    LogError($"VehiclePage: LoadVehiclesAsync CRASH: {ex.Message}\n{ex.StackTrace}");
                }
            }
            else
            {
                LogError($"VehiclePage: BindingContext is NOT VehicleViewModel: {BindingContext?.GetType()?.Name ?? "null"}");
            }
        }
        catch (Exception ex)
        {
            LogError($"VehiclePage: OnAppearing CRASH: {ex.Message}\n{ex.StackTrace}");
        }
    }
    
    private async void OnViewLogClicked(object sender, EventArgs e)
    {
        try
        {
            var logContent = ReadLog();
            var logPath = GetLogPath();
            await DisplayAlert("Log File", $"Path: {logPath}\n\nContent:\n{logContent}", "OK");
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Could not read log: {ex.Message}", "OK");
        }
    }
}