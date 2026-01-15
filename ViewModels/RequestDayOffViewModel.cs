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

    public RequestDayOffViewModel() : base()
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
}


