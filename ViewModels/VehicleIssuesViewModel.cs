﻿using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
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

    public VehicleIssuesViewModel(AuthService authService)
    {
        Title = "Vehicle Issues";
        _authService = authService;
        _issues = new ObservableCollection<VehicleIssue>();
    }

    public void Initialize(int vehicleId)
    {
        _vehicleId = vehicleId;
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
                await Application.Current.MainPage.DisplayAlert("Error", message, "OK");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"LoadIssuesAsync: Error: {ex.Message}, StackTrace: {ex.StackTrace}");
            await Application.Current.MainPage.DisplayAlert("Error", "Failed to load issues.", "OK");
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
            string[] issueTypes = { "Brakes", "Tires", "Engine", "Transmission", "Suspension", "Electrical", "Custom" };
            string? issueType = await Application.Current.MainPage.DisplayActionSheet(
                "Select Issue Type",
                "Cancel",
                null,
                issueTypes
            );

            if (string.IsNullOrEmpty(issueType) || issueType == "Cancel")
            {
                Console.WriteLine("ReportIssueAsync: Issue type selection cancelled.");
                return;
            }

            Console.WriteLine($"ReportIssueAsync: Selected issue_type={issueType}, showing description popup");
            string? description = await Application.Current.MainPage.DisplayPromptAsync(
                "Describe Issue",
                "Enter a description (optional):",
                maxLength: 500,
                keyboard: Keyboard.Text
            );

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
                await Application.Current.MainPage.DisplayAlert("Success", "Issue reported successfully.", "OK");
                await LoadIssuesAsync(); // Refresh issues
            }
            else
            {
                Console.WriteLine($"ReportIssueAsync: Failed to add issue: {message}");
                await Application.Current.MainPage.DisplayAlert("Error", message, "OK");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"ReportIssueAsync: Error: {ex.Message}, StackTrace: {ex.StackTrace}");
            await Application.Current.MainPage.DisplayAlert("Error", "Failed to report issue.", "OK");
        }
        finally
        {
            IsBusy = false;
            Console.WriteLine("ReportIssueAsync: Completed, IsBusy=false");
        }
    }

    [RelayCommand]
    async Task GoToHome()
    {
        try
        {
            Console.WriteLine("GoToHome: Navigating to Home");
            await Shell.Current.GoToAsync($"//Home?refresh={Guid.NewGuid()}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"GoToHome: Error navigating to Home: {ex.Message}");
            await Application.Current.MainPage.DisplayAlert("Error", "Failed to navigate to Home page.", "OK");
        }
    }

    [RelayCommand]
    async Task GoToVehicle()
    {
        try
        {
            Console.WriteLine("GoToVehicle: Navigating to Vehicle");
            await Shell.Current.GoToAsync($"//Vehicle?refresh={Guid.NewGuid()}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"GoToVehicle: Error navigating to Vehicle: {ex.Message}");
            await Application.Current.MainPage.DisplayAlert("Error", "Failed to navigate to Vehicle page.", "OK");
        }
    }

    [RelayCommand]
    async Task GoToIssues()
    {
        try
        {
            Console.WriteLine("GoToIssues: Navigating to Vehicle Issues");
            await Shell.Current.GoToAsync($"//VehicleIssues?refresh={Guid.NewGuid()}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"GoToIssues: Error navigating to Vehicle Issues: {ex.Message}");
            await Application.Current.MainPage.DisplayAlert("Error", "Failed to navigate to Vehicle Issues page.", "OK");
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
            await Application.Current.MainPage.DisplayAlert("Error", "Failed to navigate to Finish Day page.", "OK");
        }
    }
}