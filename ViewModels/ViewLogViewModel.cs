using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Storage;
using Newtonsoft.Json;

namespace HiatMeApp.ViewModels;

public partial class ViewLogViewModel : BaseViewModel
{
    [ObservableProperty]
    private bool _isVehicleButtonVisible;

    [ObservableProperty]
    private bool _isIssuesButtonVisible;

    public ViewLogViewModel()
    {
        Title = "Developer Tools";
        
        // Set visibility based on user role
        var userJson = Preferences.Get("UserData", string.Empty);
        if (!string.IsNullOrEmpty(userJson))
        {
            try
            {
                var user = JsonConvert.DeserializeObject<Models.User>(userJson);
                IsVehicleButtonVisible = user?.Role is "Driver" or "Manager" or "Owner";
                IsIssuesButtonVisible = user?.Role is "Driver" or "Manager" or "Owner";
            }
            catch
            {
                IsVehicleButtonVisible = false;
                IsIssuesButtonVisible = false;
            }
        }
    }

    [RelayCommand]
    private async Task GoToHome()
    {
        try
        {
            await Shell.Current.GoToAsync("//Home");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"GoToHome: Error navigating to Home: {ex.Message}");
        }
    }

    [RelayCommand]
    private async Task GoToProfile()
    {
        try
        {
            await Shell.Current.GoToAsync("//Profile");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"GoToProfile: Error navigating to Profile: {ex.Message}");
        }
    }

    [RelayCommand]
    private async Task GoToVehicle()
    {
        try
        {
            await Shell.Current.GoToAsync("//Vehicle");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"GoToVehicle: Error navigating to Vehicle: {ex.Message}");
        }
    }

    [RelayCommand]
    private async Task GoToIssues()
    {
        try
        {
            await Shell.Current.GoToAsync("//VehicleIssues");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"GoToIssues: Error navigating to Vehicle Issues: {ex.Message}");
        }
    }

    [RelayCommand]
    private async Task GoToFinishDay()
    {
        try
        {
            await Shell.Current.GoToAsync("//FinishDay");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"GoToFinishDay: Error navigating to Finish Day: {ex.Message}");
        }
    }

    [RelayCommand]
    private async Task GoToRequestDayOff()
    {
        try
        {
            await Shell.Current.GoToAsync("//RequestDayOff");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"GoToRequestDayOff: Error navigating to Request Day Off: {ex.Message}");
        }
    }

    [RelayCommand]
    private async Task GoToViewLog()
    {
        try
        {
            await Shell.Current.GoToAsync("//ViewLog");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"GoToViewLog: Error navigating to View Log: {ex.Message}");
        }
    }
}

