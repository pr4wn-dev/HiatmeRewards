using CommunityToolkit.Mvvm.ComponentModel;
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

    public async Task LoadIssuesAsync() // Public, no RelayCommand
    {
        try
        {
            IsBusy = true;
            Console.WriteLine($"LoadIssuesAsync: Fetching issues for vehicle_id={_vehicleId}");
            var (success, issues, message) = await _authService.GetVehicleIssuesAsync(_vehicleId);
            if (success && issues != null)
            {
                Issues.Clear();
                foreach (var issue in issues)
                {
                    Issues.Add(issue);
                    Console.WriteLine($"LoadIssuesAsync: Added issue: Id={issue.IssueId}, Type={issue.IssueType}, Status={issue.Status}");
                }
                Console.WriteLine($"LoadIssuesAsync: Loaded {issues.Count} issues, Issues collection count={Issues.Count}");
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
}