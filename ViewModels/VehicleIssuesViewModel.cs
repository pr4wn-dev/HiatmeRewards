using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HiatMeApp.Helpers;
using HiatMeApp.Models;
using HiatMeApp.Services;
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace HiatMeApp.ViewModels;

public partial class VehicleIssuesViewModel : BaseViewModel
{
    private readonly AuthService _authService;
    private int _vehicleId;

    [ObservableProperty]
    private ObservableCollection<VehicleIssue> _issues;

    public VehicleIssuesViewModel(AuthService authService) : base()
    {
        Title = "Vehicle Issues";
        _authService = authService;
        _issues = new ObservableCollection<VehicleIssue>();
    }

    public void Initialize(int vehicleId)
    {
        _vehicleId = vehicleId;
    }

    [RelayCommand]
    public async Task Refresh()
    {
        await LoadIssuesAsync();
    }

    public async Task LoadIssuesAsync()
    {
        try
        {
            IsBusy = true;
            Console.WriteLine($"LoadIssuesAsync: Fetching issues for vehicle_id={_vehicleId}");
            var (success, issues, message) = await _authService.GetVehicleIssuesAsync(_vehicleId);
            if (success && issues != null)
            {
                Issues.Clear();
                foreach (var issue in issues.Where(i => i.Status == "Open"))
                {
                    Issues.Add(issue);
                    Console.WriteLine($"LoadIssuesAsync: Added open issue: Id={issue.IssueId}, Type={issue.IssueType}, Status={issue.Status}");
                }
                Console.WriteLine($"LoadIssuesAsync: Loaded {issues.Count} total issues, {Issues.Count} open issues for vehicle_id={_vehicleId}");
            }
            else
            {
                Console.WriteLine($"LoadIssuesAsync: Failed to load issues: {message}");
                // Don't show another popup if it's a "logged in elsewhere" error - that popup was already shown
                if (!message.StartsWith("LOGGED_IN_ELSEWHERE:", StringComparison.OrdinalIgnoreCase))
                {
                    await PageDialogService.DisplayAlertAsync("Error", message, "OK");
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"LoadIssuesAsync: Error: {ex.Message}, StackTrace: {ex.StackTrace}");
            await PageDialogService.DisplayAlertAsync("Error", "Failed to load issues.", "OK");
        }
        finally
        {
            IsBusy = false;
            Console.WriteLine($"LoadIssuesAsync: IsBusy set to false, Issues count={Issues.Count}");
        }
    }

    public async Task ReportIssueAsync()
    {
        try
        {
            Console.WriteLine("ReportIssueAsync: Button clicked, showing issue type popup");
            string[] issueTypes = { "Brakes", "Tires", "Engine", "Transmission", "Suspension", "Electrical", "Camera Issue", "SD Card Full", "Custom" };
            string? issueType = await PageDialogService.DisplayActionSheetAsync("Select Issue Type", "Cancel", null, issueTypes);

            if (string.IsNullOrEmpty(issueType) || issueType == "Cancel")
            {
                Console.WriteLine("ReportIssueAsync: Issue type selection cancelled.");
                return;
            }

            Console.WriteLine($"ReportIssueAsync: Selected issue_type={issueType}, showing description popup");
            string? description = await PageDialogService.DisplayPromptAsync("Describe Issue", "Enter a description (optional):", maxLength: 500, keyboard: Keyboard.Text);

            if (description == null)
            {
                Console.WriteLine("ReportIssueAsync: Description input cancelled.");
                return;
            }

            IsBusy = true;
            Console.WriteLine($"ReportIssueAsync: Submitting issue for vehicle_id={_vehicleId}, issue_type={issueType}, description={description}");
            var (success, message) = await _authService.AddVehicleIssueAsync(_vehicleId, issueType, description);
            if (success)
            {
                Console.WriteLine($"ReportIssueAsync: Successfully added issue for vehicle_id={_vehicleId}");
                await PageDialogService.DisplayAlertAsync("Success", "Issue reported successfully.", "OK");
                await LoadIssuesAsync(); // Refresh issues
            }
            else
            {
                Console.WriteLine($"ReportIssueAsync: Failed to add issue: {message}");
                // Don't show another popup if it's a "logged in elsewhere" error - that popup was already shown
                if (!message.StartsWith("LOGGED_IN_ELSEWHERE:", StringComparison.OrdinalIgnoreCase))
                {
                    await PageDialogService.DisplayAlertAsync("Error", message, "OK");
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"ReportIssueAsync: Error: {ex.Message}, StackTrace: {ex.StackTrace}");
            await PageDialogService.DisplayAlertAsync("Error", "Failed to report issue.", "OK");
        }
        finally
        {
            IsBusy = false;
            Console.WriteLine("ReportIssueAsync: Completed, IsBusy=false");
        }
    }
}