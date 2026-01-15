using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Newtonsoft.Json;
using HiatMeApp.Models;
using System;

namespace HiatMeApp.ViewModels;

public partial class BaseViewModel : ObservableObject
{
    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private string? _title;

    [ObservableProperty]
    private bool _isClient;

    [ObservableProperty]
    private bool _isVehicleButtonVisible;

    [ObservableProperty]
    private bool _isIssuesButtonVisible;

    public BaseViewModel()
    {
        // Initialize role-based visibility from stored user data
        InitializeRoleBasedVisibility();
    }

    protected void InitializeRoleBasedVisibility()
    {
        var userJson = Preferences.Get("UserData", string.Empty);
        if (!string.IsNullOrEmpty(userJson))
        {
            try
            {
                var user = JsonConvert.DeserializeObject<User>(userJson);
                if (user != null)
                {
                    IsClient = user.Role?.Equals("Client", StringComparison.OrdinalIgnoreCase) == true;
                    IsVehicleButtonVisible = user.Role is "Driver" or "Manager" or "Owner";
                    IsIssuesButtonVisible = user.Role is "Driver" or "Manager" or "Owner";
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"BaseViewModel: Error parsing user data: {ex.Message}");
            }
        }
    }

    [RelayCommand]
    protected virtual async Task GoToHome()
    {
        try
        {
            await Shell.Current.GoToAsync($"//Home?refresh={Guid.NewGuid()}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"GoToHome: Error: {ex.Message}");
        }
    }

    [RelayCommand]
    protected virtual async Task GoToProfile()
    {
        try
        {
            await Shell.Current.GoToAsync($"//Profile?refresh={Guid.NewGuid()}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"GoToProfile: Error: {ex.Message}");
        }
    }

    [RelayCommand]
    protected virtual async Task GoToRequestDayOff()
    {
        try
        {
            await Shell.Current.GoToAsync($"//RequestDayOff?refresh={Guid.NewGuid()}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"GoToRequestDayOff: Error: {ex.Message}");
        }
    }

    [RelayCommand]
    protected virtual async Task GoToVehicle()
    {
        try
        {
            await Shell.Current.GoToAsync($"//Vehicle?refresh={Guid.NewGuid()}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"GoToVehicle: Error: {ex.Message}");
        }
    }

    [RelayCommand]
    protected virtual async Task GoToIssues()
    {
        try
        {
            await Shell.Current.GoToAsync($"//VehicleIssues?refresh={Guid.NewGuid()}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"GoToIssues: Error: {ex.Message}");
        }
    }

    [RelayCommand]
    protected virtual async Task GoToFinishDay()
    {
        try
        {
            await Shell.Current.GoToAsync($"//FinishDay?refresh={Guid.NewGuid()}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"GoToFinishDay: Error: {ex.Message}");
        }
    }

    [RelayCommand]
    protected virtual async Task GoToViewLog()
    {
        try
        {
            await Shell.Current.GoToAsync("//ViewLog");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"GoToViewLog: Error: {ex.Message}");
        }
    }
}