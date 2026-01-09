using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HiatMeApp.Helpers;
using HiatMeApp.Models;
using HiatMeApp.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui.Controls;
using System;
using System.Collections.ObjectModel;
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

    [ObservableProperty]
    private ObservableCollection<DayOffRequest> _myRequests = new();

    [ObservableProperty]
    private bool _isLoadingRequests;

    [ObservableProperty]
    private bool _hasRequests;

    [ObservableProperty]
    private string? _requestsErrorMessage;

    [ObservableProperty]
    private bool _isFormVisible = false; // Hidden by default so requests are visible first

    /// <summary>
    /// Text for the toggle button based on form visibility
    /// </summary>
    public string FormToggleButtonText => IsFormVisible ? "▲ Hide Request Form" : "▼ New Request";

    partial void OnIsFormVisibleChanged(bool value)
    {
        OnPropertyChanged(nameof(FormToggleButtonText));
    }

    [RelayCommand]
    private void ToggleForm()
    {
        IsFormVisible = !IsFormVisible;
    }

    public RequestDayOffViewModel()
    {
        Title = "Request Day Off";
        _authService = App.Services.GetRequiredService<AuthService>();
        MinimumDate = DateTime.Today;
        SelectedDate = DateTime.Today.AddDays(1);
    }

    /// <summary>
    /// Load the user's day off requests from the server
    /// </summary>
    [RelayCommand]
    public async Task LoadMyRequestsAsync()
    {
        if (IsLoadingRequests) return;

        try
        {
            IsLoadingRequests = true;
            RequestsErrorMessage = null;
            Console.WriteLine("LoadMyRequestsAsync: Loading requests...");

            var (success, requests, message) = await _authService.GetMyDayOffRequestsAsync();

            if (success && requests != null)
            {
                MyRequests.Clear();
                foreach (var request in requests)
                {
                    MyRequests.Add(request);
                }
                HasRequests = MyRequests.Count > 0;
                Console.WriteLine($"LoadMyRequestsAsync: Loaded {MyRequests.Count} requests");
            }
            else
            {
                Console.WriteLine($"LoadMyRequestsAsync: Failed - {message}");
                // Don't show error popup if it's logged in elsewhere
                if (!message.StartsWith("LOGGED_IN_ELSEWHERE:", StringComparison.OrdinalIgnoreCase))
                {
                    RequestsErrorMessage = message;
                }
                HasRequests = false;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"LoadMyRequestsAsync: Error - {ex.Message}");
            RequestsErrorMessage = "Failed to load requests.";
            HasRequests = false;
        }
        finally
        {
            IsLoadingRequests = false;
        }
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
                
                // Reload requests to show the new one
                await LoadMyRequestsAsync();
            }
            else
            {
                // Don't show another popup if it's a "logged in elsewhere" error - that popup was already shown
                if (!message.StartsWith("LOGGED_IN_ELSEWHERE:", StringComparison.OrdinalIgnoreCase))
                {
                    await PageDialogService.DisplayAlertAsync("Request Day Off", message, "OK");
                }
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


