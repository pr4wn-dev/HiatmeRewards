using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HiatMeApp.Models;
using HiatMeApp.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui.Controls;
using Newtonsoft.Json;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace HiatMeApp.ViewModels;

public partial class HomeViewModel : BaseViewModel
{
    [ObservableProperty]
    private bool _isVehicleButtonVisible;

    [ObservableProperty]
    private Vehicle? _assignedVehicle;

    public HomeViewModel()
    {
        Title = "Home";
        var userJson = Preferences.Get("UserData", string.Empty);
        if (!string.IsNullOrEmpty(userJson))
        {
            var user = JsonConvert.DeserializeObject<User>(userJson);
            App.CurrentUser = user;
            IsVehicleButtonVisible = user?.Role is "Driver" or "Manager" or "Owner";
            Console.WriteLine($"HomeViewModel: Initialized with IsVehicleButtonVisible={IsVehicleButtonVisible}, Role={user?.Role}, UserId={user?.UserId}, VehiclesCount={user?.Vehicles?.Count ?? 0}");
        }
        MessagingCenter.Subscribe<VehicleViewModel, Vehicle>(this, "VehicleAssigned", (sender, vehicle) =>
        {
            Console.WriteLine($"HomeViewModel: Received VehicleAssigned message, VIN ending={vehicle.LastSixVin}");
            AssignedVehicle = vehicle;
            OnPropertyChanged(nameof(AssignedVehicle));
        });
        CheckVehicleAssignmentAsync();
    }

    private async void CheckVehicleAssignmentAsync()
    {
        try
        {
            if (App.CurrentUser == null || App.CurrentUser.Vehicles == null || !App.CurrentUser.Vehicles.Any())
            {
                Console.WriteLine("CheckVehicleAssignmentAsync: No vehicles assigned or user not logged in, navigating to Vehicle page.");
                AssignedVehicle = null;
                OnPropertyChanged(nameof(AssignedVehicle));
                await Shell.Current.GoToAsync("Vehicle");
                await Application.Current.MainPage.DisplayAlert("No Vehicle", "No vehicles assigned. Please select a vehicle.", "OK");
                return;
            }

            var assignedVehicles = App.CurrentUser.Vehicles
                .Where(v => v.CurrentUserId == App.CurrentUser.UserId)
                .ToList();
            Console.WriteLine($"CheckVehicleAssignmentAsync: Found {assignedVehicles.Count} assigned vehicles for UserId={App.CurrentUser.UserId}");

            if (assignedVehicles.Any())
            {
                var selectedVehicle = assignedVehicles.First();
                if (AssignedVehicle?.VehicleId != selectedVehicle.VehicleId)
                {
                    AssignedVehicle = selectedVehicle;
                    OnPropertyChanged(nameof(AssignedVehicle));
                }

                bool isStillInVehicle = await Application.Current.MainPage.DisplayAlert(
                    "Vehicle Confirmation",
                    $"Is this your current vehicle: {AssignedVehicle.Make} {AssignedVehicle.Model} (VIN ending {AssignedVehicle.LastSixVin})?",
                    "Yes",
                    "No"
                );

                if (!isStillInVehicle)
                {
                    AssignedVehicle = null;
                    OnPropertyChanged(nameof(AssignedVehicle));
                    App.CurrentUser.Vehicles = App.CurrentUser.Vehicles;
                    Preferences.Set("UserData", JsonConvert.SerializeObject(App.CurrentUser));
                    Console.WriteLine("CheckVehicleAssignmentAsync: User denied vehicle, navigating to Vehicle page.");
                    await Shell.Current.GoToAsync("Vehicle");
                    return;
                }

                if (AssignedVehicle.MileageRecord != null && AssignedVehicle.MileageRecord.StartMiles != null && AssignedVehicle.MileageRecord.EndingMiles == null)
                {
                    Console.WriteLine($"CheckVehicleAssignmentAsync: Open mileage record found for vehicle_id={AssignedVehicle.VehicleId}, mileage_id={AssignedVehicle.MileageRecord.MileageId}");
                    return;
                }

                if (AssignedVehicle.MileageRecord != null && AssignedVehicle.MileageRecord.StartMiles == null && AssignedVehicle.MileageRecord.MileageId != 0)
                {
                    Console.WriteLine($"CheckVehicleAssignmentAsync: Prompting for start miles for vehicle_id={AssignedVehicle.VehicleId}, mileage_id={AssignedVehicle.MileageRecord.MileageId}");
                    string? startMilesInput = await Application.Current.MainPage.DisplayPromptAsync(
                        "Start Mileage Required",
                        $"Please enter the starting miles for {AssignedVehicle.Make} {AssignedVehicle.Model} (VIN ending {AssignedVehicle.LastSixVin}):",
                        maxLength: 8,
                        keyboard: Keyboard.Numeric
                    );

                    if (string.IsNullOrEmpty(startMilesInput) || !double.TryParse(startMilesInput, out double startMiles) || startMiles < 0 || startMiles > 999999.99)
                    {
                        Console.WriteLine("CheckVehicleAssignmentAsync: Invalid or cancelled start miles input.");
                        await Application.Current.MainPage.DisplayAlert("Error", "Invalid starting miles. Please try again.", "OK");
                        return;
                    }

                    var authService = App.Services.GetRequiredService<AuthService>();
                    (bool submitSuccess, string submitMessage, int? returnedMileageId) = await authService.SubmitStartMileageAsync(AssignedVehicle.MileageRecord.MileageId, startMiles);

                    if (submitSuccess)
                    {
                        Console.WriteLine($"CheckVehicleAssignmentAsync: Successfully submitted start miles for mileage_id={returnedMileageId}, vehicle_id={AssignedVehicle.VehicleId}");
                        await Application.Current.MainPage.DisplayAlert("Success", "Starting miles submitted successfully.", "OK");
                        AssignedVehicle.MileageRecord.StartMiles = (float?)startMiles;
                        AssignedVehicle.MileageRecord.StartMilesDatetime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                        App.CurrentUser.Vehicles = App.CurrentUser.Vehicles;
                        Preferences.Set("UserData", JsonConvert.SerializeObject(App.CurrentUser));
                        OnPropertyChanged(nameof(AssignedVehicle));
                        MessagingCenter.Send(this, "VehicleAssigned", AssignedVehicle);
                    }
                    else
                    {
                        Console.WriteLine($"CheckVehicleAssignmentAsync: Failed to submit start miles: {submitMessage}");
                        string errorMessage = submitMessage;
                        if (submitMessage.Contains("Too many attempts"))
                        {
                            errorMessage = "Rate limit exceeded. Please wait 15 minutes and try again.";
                        }
                        else if (submitMessage.Contains("Invalid mileage ID") || submitMessage.Contains("Mileage record not owned"))
                        {
                            errorMessage = "Failed to submit starting miles. Please try again or reassign the vehicle.";
                        }
                        await Application.Current.MainPage.DisplayAlert("Error", errorMessage, "OK");
                    }
                }
                else
                {
                    Console.WriteLine($"CheckVehicleAssignmentAsync: No valid mileage record, reassigning vehicle with VIN ending {AssignedVehicle.LastSixVin}.");
                    string vinSuffix = AssignedVehicle.LastSixVin ?? string.Empty;
                    if (string.IsNullOrEmpty(vinSuffix))
                    {
                        Console.WriteLine("CheckVehicleAssignmentAsync: Invalid or empty VIN suffix.");
                        await Application.Current.MainPage.DisplayAlert("Error", "Vehicle VIN is invalid.", "OK");
                        await Shell.Current.GoToAsync("Vehicle");
                        return;
                    }

                    var authService = App.Services.GetRequiredService<AuthService>();
                    (bool success, Vehicle? newVehicle, string message, List<MileageRecord>? incompleteRecords, int? mileageId) = await authService.AssignVehicleAsync(vinSuffix);

                    if (success && newVehicle != null && mileageId.HasValue)
                    {
                        Console.WriteLine($"CheckVehicleAssignmentAsync: Reassigned vehicle, VIN={newVehicle.Vin}, VehicleId={newVehicle.VehicleId}, MileageId={mileageId}");
                        var updatedVehicles = App.CurrentUser.Vehicles
                            .Where(v => v.VehicleId != newVehicle.VehicleId)
                            .ToList();
                        updatedVehicles.Add(newVehicle);
                        App.CurrentUser.Vehicles = updatedVehicles;
                        AssignedVehicle = newVehicle;
                        Preferences.Set("UserData", JsonConvert.SerializeObject(App.CurrentUser));
                        OnPropertyChanged(nameof(AssignedVehicle));
                        MessagingCenter.Send(this, "VehicleAssigned", newVehicle);

                        string? startMilesInput = await Application.Current.MainPage.DisplayPromptAsync(
                            "New Mileage Entry",
                            $"Please enter the starting miles for {newVehicle.Make} {newVehicle.Model} (VIN ending {newVehicle.LastSixVin}):",
                            maxLength: 8,
                            keyboard: Keyboard.Numeric
                        );

                        if (string.IsNullOrEmpty(startMilesInput) || !double.TryParse(startMilesInput, out double startMiles) || startMiles < 0 || startMiles > 999999.99)
                        {
                            Console.WriteLine("CheckVehicleAssignmentAsync: Invalid or cancelled start miles input.");
                            await Application.Current.MainPage.DisplayAlert("Error", "Invalid starting miles. Please try again.", "OK");
                            return;
                        }

                        (bool submitSuccess, string submitMessage, int? returnedMileageId) = await authService.SubmitStartMileageAsync(mileageId.Value, startMiles);
                        if (submitSuccess)
                        {
                            Console.WriteLine($"CheckVehicleAssignmentAsync: Successfully submitted start miles for mileage_id={returnedMileageId}, vehicle_id={newVehicle.VehicleId}");
                            await Application.Current.MainPage.DisplayAlert("Success", "Vehicle reassigned and starting miles submitted successfully.", "OK");
                            newVehicle.MileageRecord = new MileageRecord
                            {
                                MileageId = returnedMileageId ?? 0,
                                VehicleId = newVehicle.VehicleId,
                                UserId = App.CurrentUser.UserId,
                                StartMiles = (float?)startMiles,
                                StartMilesDatetime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
                            };
                            App.CurrentUser.Vehicles = updatedVehicles;
                            AssignedVehicle = newVehicle;
                            Preferences.Set("UserData", JsonConvert.SerializeObject(App.CurrentUser));
                            OnPropertyChanged(nameof(AssignedVehicle));
                            MessagingCenter.Send(this, "VehicleAssigned", newVehicle);
                        }
                        else
                        {
                            Console.WriteLine($"CheckVehicleAssignmentAsync: Failed to submit start miles: {submitMessage}");
                            string errorMessage = submitMessage;
                            if (submitMessage.Contains("Too many attempts"))
                            {
                                errorMessage = "Rate limit exceeded. Please wait 15 minutes and try again.";
                            }
                            else if (submitMessage.Contains("Invalid mileage ID") || submitMessage.Contains("Mileage record not owned"))
                            {
                                errorMessage = "Failed to submit starting miles. Please try assigning the vehicle again.";
                            }
                            await Application.Current.MainPage.DisplayAlert("Error", errorMessage, "OK");
                        }
                    }
                    else if (incompleteRecords != null && incompleteRecords.Any())
                    {
                        Console.WriteLine($"CheckVehicleAssignmentAsync: Found {incompleteRecords.Count} incomplete mileage records.");
                        foreach (var record in incompleteRecords)
                        {
                            bool needsStartMiles = record.MissingFields?.Contains("start_miles") == true || record.MissingFields?.Contains("start_miles_datetime") == true;
                            if (needsStartMiles)
                            {
                                string? startMilesInput = await Application.Current.MainPage.DisplayPromptAsync(
                                    "Incomplete Mileage",
                                    $"Please enter the starting miles for vehicle ID {record.VehicleId}:",
                                    maxLength: 8,
                                    keyboard: Keyboard.Numeric
                                );

                                if (string.IsNullOrEmpty(startMilesInput) || !double.TryParse(startMilesInput, out double startMiles) || startMiles < 0 || startMiles > 999999.99)
                                {
                                    Console.WriteLine($"CheckVehicleAssignmentAsync: Invalid start miles input for mileage_id={record.MileageId}.");
                                    await Application.Current.MainPage.DisplayAlert("Error", "Invalid starting miles. Please try again.", "OK");
                                    continue;
                                }

                                (bool submitSuccess, string submitMessage, int? returnedMileageId) = await authService.SubmitStartMileageAsync(record.MileageId, startMiles);
                                if (submitSuccess)
                                {
                                    Console.WriteLine($"CheckVehicleAssignmentAsync: Successfully submitted start miles for mileage_id={returnedMileageId}, vehicle_id={record.VehicleId}");
                                    await Application.Current.MainPage.DisplayAlert("Success", "Starting miles submitted successfully.", "OK");
                                    var updatedVehicle = App.CurrentUser.Vehicles.FirstOrDefault(v => v.VehicleId == record.VehicleId);
                                    if (updatedVehicle != null)
                                    {
                                        updatedVehicle.MileageRecord = new MileageRecord
                                        {
                                            MileageId = returnedMileageId ?? 0,
                                            VehicleId = record.VehicleId,
                                            UserId = App.CurrentUser.UserId,
                                            StartMiles = (float?)startMiles,
                                            StartMilesDatetime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
                                        };
                                        App.CurrentUser.Vehicles = App.CurrentUser.Vehicles;
                                        if (updatedVehicle.CurrentUserId == App.CurrentUser.UserId)
                                        {
                                            AssignedVehicle = updatedVehicle;
                                            OnPropertyChanged(nameof(AssignedVehicle));
                                            MessagingCenter.Send(this, "VehicleAssigned", updatedVehicle);
                                        }
                                        Preferences.Set("UserData", JsonConvert.SerializeObject(App.CurrentUser));
                                    }
                                }
                                else
                                {
                                    Console.WriteLine($"CheckVehicleAssignmentAsync: Failed to submit start miles for mileage_id={record.MileageId}: {submitMessage}");
                                    string errorMessage = submitMessage;
                                    if (submitMessage.Contains("Too many attempts"))
                                    {
                                        errorMessage = "Rate limit exceeded. Please wait 15 minutes and try again.";
                                    }
                                    await Application.Current.MainPage.DisplayAlert("Error", errorMessage, "OK");
                                }
                            }
                        }
                    }
                    else
                    {
                        Console.WriteLine($"CheckVehicleAssignmentAsync: Failed to reassign vehicle: {message}");
                        await Application.Current.MainPage.DisplayAlert("Error", message, "OK");
                        await Shell.Current.GoToAsync("Vehicle");
                    }
                }
            }
            else
            {
                Console.WriteLine("CheckVehicleAssignmentAsync: No assigned vehicles, navigating to Vehicle page.");
                AssignedVehicle = null;
                OnPropertyChanged(nameof(AssignedVehicle));
                await Shell.Current.GoToAsync("Vehicle");
                await Application.Current.MainPage.DisplayAlert("No Vehicle", "No vehicles assigned. Please select a vehicle.", "OK");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"CheckVehicleAssignmentAsync: Error: {ex.Message}, StackTrace: {ex.StackTrace}");
            await Application.Current.MainPage.DisplayAlert("Error", "Failed to check vehicle.", "OK");
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
            await Application.Current.MainPage.DisplayAlert("Error", "Failed to navigate to Home page.", "OK");
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
            await Application.Current.MainPage.DisplayAlert("Error", "Failed to navigate to Vehicle page.", "OK");
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
            await Application.Current.MainPage.DisplayAlert("Error", "Failed to navigate to Vehicle Issues page.", "OK");
        }
    }
}