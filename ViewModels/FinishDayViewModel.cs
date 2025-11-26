using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using HiatMeApp.Helpers;
using HiatMeApp.Messages;
using HiatMeApp.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Dispatching;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace HiatMeApp.ViewModels;

public partial class FinishDayViewModel : BaseViewModel, IDisposable
{
    private readonly AuthService _authService;
    private int? _vehicleId;
    private double? _startingMiles;
    private bool _disposed;

    public FinishDayViewModel()
    {
        Title = "Finish Day";
        _authService = App.Services.GetRequiredService<AuthService>();

        WeakReferenceMessenger.Default.Register<FinishDayViewModel, VehicleAssignedMessage>(this, (recipient, message) =>
        {
            _ = recipient.LoadAssignedVehicleAsync();
        });

        MainThread.BeginInvokeOnMainThread(async () => await LoadAssignedVehicleAsync());
    }

    public void Dispose()
    {
        if (_disposed) return;
        WeakReferenceMessenger.Default.UnregisterAll(this);
        _disposed = true;
    }

    private async Task LoadAssignedVehicleAsync()
    {
        try
        {
            var user = App.CurrentUser;
            var vehicle = user?.Vehicles?.FirstOrDefault(v => v.CurrentUserId == user.UserId);

            if (vehicle?.MileageRecord?.StartMiles == null)
            {
                await ShowAlertAsync("Finish Day", "No active vehicle or mileage record found.");
                await Shell.Current.GoToAsync("//Home");
                return;
            }

            _vehicleId = vehicle.VehicleId;
            _startingMiles = vehicle.MileageRecord.StartMiles;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"LoadAssignedVehicleAsync: {ex}");
            await ShowAlertAsync("Finish Day", "Unable to load vehicle data.");
            await Shell.Current.GoToAsync("//Home");
        }
    }

    [RelayCommand]
    private async Task SubmitEndOfDay()
    {
        try
        {
            if (IsBusy)
                return;

            if (!_vehicleId.HasValue || !_startingMiles.HasValue)
            {
                await ShowAlertAsync("Finish Day", "No vehicle assignment found.");
                return;
            }

            IsBusy = true;

            var promptResult = await ShowPromptAsync("Ending Mileage", "Enter the ending mileage for your vehicle:");

            if (string.IsNullOrWhiteSpace(promptResult) || !double.TryParse(promptResult, out double endingMiles))
            {
                await ShowAlertAsync("Finish Day", "Please enter a valid number.");
                return;
            }

            if (endingMiles <= _startingMiles.Value)
            {
                await ShowAlertAsync("Finish Day", "Ending mileage must be greater than the starting mileage.");
                return;
            }

            var (success, message, _) = await _authService.SubmitEndMileageAsync(_vehicleId.Value, endingMiles);

            if (!success)
            {
                await ShowAlertAsync("Finish Day", message);
                return;
            }

            await ShowAlertAsync("Finish Day", "Ending mileage submitted successfully.");
            await Shell.Current.GoToAsync($"//Home?refresh={Guid.NewGuid()}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"SubmitEndOfDay: {ex}");
            await ShowAlertAsync("Finish Day", "Failed to submit ending mileage.");
        }
        finally
        {
            IsBusy = false;
        }
    }

    private static Page? GetActivePage()
    {
        return Application.Current?.Windows.FirstOrDefault()?.Page;
    }

    private static async Task ShowAlertAsync(string title, string message)
    {
        var page = GetActivePage();
        if (page != null)
        {
            await page.DisplayAlert(title, message, "OK");
        }
    }

    private static async Task<string?> ShowPromptAsync(string title, string message)
    {
        var page = GetActivePage();
        if (page == null)
            return null;

        return await page.DisplayPromptAsync(title, message, keyboard: Keyboard.Numeric);
    }
    [RelayCommand]
    private async Task GoToHome()
    {
        try
        {
            Console.WriteLine("GoToHome: Navigating to Home");
            await Shell.Current.GoToAsync($"//Home?refresh={Guid.NewGuid()}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"GoToHome: Error navigating to Home: {ex.Message}");
            await PageDialogService.DisplayAlertAsync("Error", "Failed to navigate to Home page.", "OK");
        }
    }

    [RelayCommand]
    private async Task GoToVehicle()
    {
        try
        {
            Console.WriteLine("GoToVehicle: Navigating to Vehicle");
            await Shell.Current.GoToAsync($"//Vehicle?refresh={Guid.NewGuid()}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"GoToVehicle: Error navigating to Vehicle: {ex.Message}");
            await PageDialogService.DisplayAlertAsync("Error", "Failed to navigate to Vehicle page.", "OK");
        }
    }

    [RelayCommand]
    private async Task GoToIssues()
    {
        try
        {
            Console.WriteLine("GoToIssues: Navigating to Vehicle Issues");
            await Shell.Current.GoToAsync($"//VehicleIssues?refresh={Guid.NewGuid()}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"GoToIssues: Error navigating to Vehicle Issues: {ex.Message}");
            await PageDialogService.DisplayAlertAsync("Error", "Failed to navigate to Vehicle Issues page.", "OK");
        }
    }

    [RelayCommand]
    private async Task GoToFinishDay()
    {
        try
        {
            Console.WriteLine("GoToFinishDay: Navigating to Finish Day");
            await Shell.Current.GoToAsync($"//FinishDay?refresh={Guid.NewGuid()}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"GoToFinishDay: Error navigating to Finish Day: {ex.Message}");
            await PageDialogService.DisplayAlertAsync("Error", "Failed to navigate to Finish Day page.", "OK");
        }
    }
}