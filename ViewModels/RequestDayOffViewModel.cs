using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HiatMeApp.Helpers;
using HiatMeApp.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui.Controls;
using System;
using System.Threading.Tasks;

namespace HiatMeApp.ViewModels;

public partial class RequestDayOffViewModel : BaseViewModel
{
    private readonly AuthService _authService;

    [ObservableProperty]
    private DateTime _selectedDate;

    [ObservableProperty]
    private DateTime _minimumDate;

    [ObservableProperty]
    private string? _reason;

    [ObservableProperty]
    private TimeSpan? _startTime;

    [ObservableProperty]
    private TimeSpan? _endTime;

    public RequestDayOffViewModel()
    {
        Title = "Request Day Off";
        _authService = App.Services.GetRequiredService<AuthService>();
        MinimumDate = DateTime.Today;
        SelectedDate = DateTime.Today.AddDays(1);
    }

    [RelayCommand]
    private async Task SubmitRequest()
    {
        try
        {
            if (IsBusy)
                return;

            IsBusy = true;

            if (StartTime.HasValue && EndTime.HasValue && StartTime.Value >= EndTime.Value)
            {
                await PageDialogService.DisplayAlertAsync("Request Day Off", "End time must be after start time.", "OK");
                return;
            }

            var (success, message) = await _authService.SubmitDayOffRequestAsync(SelectedDate, Reason, StartTime, EndTime);

            if (success)
            {
                await PageDialogService.DisplayAlertAsync("Request Day Off", "Your request has been submitted.", "OK");
                Reason = string.Empty;
                OnPropertyChanged(nameof(Reason));
                StartTime = null;
                EndTime = null;
                OnPropertyChanged(nameof(StartTime));
                OnPropertyChanged(nameof(EndTime));
            }
            else
            {
                await PageDialogService.DisplayAlertAsync("Request Day Off", message, "OK");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"SubmitRequest: Error submitting day off request: {ex.Message}");
            await PageDialogService.DisplayAlertAsync("Request Day Off", "Failed to submit your request.", "OK");
        }
        finally
        {
            IsBusy = false;
        }
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

    [RelayCommand]
    private async Task GoToProfile()
    {
        try
        {
            Console.WriteLine("GoToProfile: Navigating to Profile");
            await Shell.Current.GoToAsync($"//Profile?refresh={Guid.NewGuid()}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"GoToProfile: Error navigating to Profile: {ex.Message}");
            await PageDialogService.DisplayAlertAsync("Error", "Failed to navigate to Profile page.", "OK");
        }
    }

    [RelayCommand]
    private async Task GoToRequestDayOff()
    {
        try
        {
            Console.WriteLine("GoToRequestDayOff: Navigating to Request Day Off");
            await Shell.Current.GoToAsync($"//RequestDayOff?refresh={Guid.NewGuid()}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"GoToRequestDayOff: Error navigating to Request Day Off: {ex.Message}");
            await PageDialogService.DisplayAlertAsync("Error", "Failed to navigate to Request Day Off page.", "OK");
        }
    }

    [RelayCommand]
    private async Task GoToViewLog()
    {
        try
        {
            Console.WriteLine("GoToViewLog: Navigating to View Log");
            await Shell.Current.GoToAsync("//ViewLog");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"GoToViewLog: Error navigating to View Log: {ex.Message}");
            await PageDialogService.DisplayAlertAsync("Error", "Failed to navigate to View Log page.", "OK");
        }
    }
}


