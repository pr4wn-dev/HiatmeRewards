using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Threading.Tasks;
using HiatMeApp.Models;

namespace HiatMeApp.ViewModels;

public partial class FinishDayViewModel : BaseViewModel
{
    public FinishDayViewModel()
    {
        Title = "Finish Day";
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
    async Task GoToFinishDay()
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