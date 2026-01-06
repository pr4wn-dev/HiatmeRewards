using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using HiatMeApp.Helpers;
using HiatMeApp.Messages;
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
        WeakReferenceMessenger.Default.Register<HomeViewModel, VehicleAssignedMessage>(this, (recipient, message) =>
        {
            Console.WriteLine($"HomeViewModel: Received VehicleAssigned message, VIN ending={message.Value.LastSixVin}");
            recipient.AssignedVehicle = message.Value;
            recipient.OnPropertyChanged(nameof(AssignedVehicle));
        });
        CheckVehicleAssignmentAsync();
    }

    private async void CheckVehicleAssignmentAsync()
    {
        try
        {
            // Small delay to ensure ValidateSessionAsync has completed and saved the CSRF token
            await Task.Delay(500);
            
            if (App.CurrentUser == null || App.CurrentUser.Vehicles == null || !App.CurrentUser.Vehicles.Any())
            {
                Console.WriteLine("CheckVehicleAssignmentAsync: No vehicles assigned or user not logged in, navigating to Vehicle page.");
                AssignedVehicle = null;
                OnPropertyChanged(nameof(AssignedVehicle));
                await Shell.Current.GoToAsync("Vehicle");
                await PageDialogService.DisplayAlertAsync("No Vehicle", "No vehicles assigned. Please select a vehicle.", "OK");
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

                // Only show vehicle confirmation if we just logged in
                bool shouldConfirm = Preferences.Get("ShouldConfirmVehicle", false);
                if (shouldConfirm)
                {
                    bool isStillInVehicle = await PageDialogService.DisplayAlertAsync(
                        "Vehicle Confirmation",
                        $"Is this your current vehicle: {AssignedVehicle.Make} {AssignedVehicle.Model} (VIN ending {AssignedVehicle.LastSixVin})?",
                        "Yes",
                        "No"
                    );

                    // If user clicks "No", they don't have this vehicle - clear assignment
                    if (!isStillInVehicle)
                    {
                        Console.WriteLine("CheckVehicleAssignmentAsync: User denied vehicle confirmation");
                        
                        // Remove this vehicle from current user assignment (set CurrentUserId to null or remove from list)
                        // We'll update the vehicle list to remove this assignment
                        var updatedVehicles = App.CurrentUser.Vehicles
                            .Where(v => !(v.VehicleId == AssignedVehicle.VehicleId && v.CurrentUserId == App.CurrentUser.UserId))
                            .ToList();
                        App.CurrentUser.Vehicles = updatedVehicles;
                        
                        AssignedVehicle = null;
                        OnPropertyChanged(nameof(AssignedVehicle));
                        Preferences.Set("UserData", JsonConvert.SerializeObject(App.CurrentUser));
                        Console.WriteLine("CheckVehicleAssignmentAsync: Vehicle assignment cleared, navigating to Vehicle page.");
                        Preferences.Set("ShouldConfirmVehicle", false); // Clear flag after confirmation
                        await Shell.Current.GoToAsync("Vehicle");
                        return;
                    }
                    // Clear the flag after showing confirmation once (user clicked "Yes")
                    Preferences.Set("ShouldConfirmVehicle", false);
                }

                if (AssignedVehicle.MileageRecord != null && AssignedVehicle.MileageRecord.StartMiles != null && AssignedVehicle.MileageRecord.EndingMiles == null)
                {
                    Console.WriteLine($"CheckVehicleAssignmentAsync: Open mileage record found for vehicle_id={AssignedVehicle.VehicleId}, mileage_id={AssignedVehicle.MileageRecord.MileageId}");
                    return;
                }

                if (AssignedVehicle.MileageRecord != null && AssignedVehicle.MileageRecord.StartMiles == null && AssignedVehicle.MileageRecord.MileageId != 0)
                {
                    Console.WriteLine($"CheckVehicleAssignmentAsync: Prompting for start miles for vehicle_id={AssignedVehicle.VehicleId}, mileage_id={AssignedVehicle.MileageRecord.MileageId}");
                    string? startMilesInput = await PageDialogService.DisplayPromptAsync(
                        "Start Mileage Required",
                        $"Please enter the starting miles for {AssignedVehicle.Make} {AssignedVehicle.Model} (VIN ending {AssignedVehicle.LastSixVin}):",
                        maxLength: 8,
                        keyboard: Keyboard.Numeric
                    );

                    if (string.IsNullOrEmpty(startMilesInput) || !double.TryParse(startMilesInput, out double startMiles) || startMiles < 0 || startMiles > 999999.99)
                    {
                        Console.WriteLine("CheckVehicleAssignmentAsync: Invalid or cancelled start miles input.");
                        await PageDialogService.DisplayAlertAsync("Error", "Invalid starting miles. Please try again.", "OK");
                        return;
                    }

                    var authService = App.Services.GetRequiredService<AuthService>();
                    (bool submitSuccess, string submitMessage, int? returnedMileageId) = await authService.SubmitStartMileageAsync(AssignedVehicle.MileageRecord.MileageId, startMiles);

                    if (submitSuccess)
                    {
                        Console.WriteLine($"CheckVehicleAssignmentAsync: Successfully submitted start miles for mileage_id={returnedMileageId}, vehicle_id={AssignedVehicle.VehicleId}");
                        await PageDialogService.DisplayAlertAsync("Success", "Starting miles submitted successfully.", "OK");
                        AssignedVehicle.MileageRecord.StartMiles = (float?)startMiles;
                        AssignedVehicle.MileageRecord.StartMilesDatetime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                        App.CurrentUser.Vehicles = App.CurrentUser.Vehicles;
                        Preferences.Set("UserData", JsonConvert.SerializeObject(App.CurrentUser));
                        OnPropertyChanged(nameof(AssignedVehicle));
                        if (AssignedVehicle != null)
                        {
                            WeakReferenceMessenger.Default.Send(new VehicleAssignedMessage(AssignedVehicle));
                        }
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
                        else if (submitMessage.Contains("CSRF token") || submitMessage.Contains("Invalid CSRF") || submitMessage.Contains("csrf") || submitMessage.Contains("session token"))
                        {
                            // CSRF token error - show user-friendly message
                            errorMessage = "Session expired. Please close and reopen the app.";
                        }
                        await PageDialogService.DisplayAlertAsync("Error", errorMessage, "OK");
                    }
                }
                else
                {
                    Console.WriteLine($"CheckVehicleAssignmentAsync: No valid mileage record, reassigning vehicle with VIN ending {AssignedVehicle.LastSixVin}.");
                    string vinSuffix = AssignedVehicle.LastSixVin ?? string.Empty;
                    if (string.IsNullOrEmpty(vinSuffix))
                    {
                        Console.WriteLine("CheckVehicleAssignmentAsync: Invalid or empty VIN suffix.");
                        await PageDialogService.DisplayAlertAsync("Error", "Vehicle VIN is invalid.", "OK");
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
                        WeakReferenceMessenger.Default.Send(new VehicleAssignedMessage(newVehicle));

                        string? startMilesInput = await PageDialogService.DisplayPromptAsync(
                            "New Mileage Entry",
                            $"Please enter the starting miles for {newVehicle.Make} {newVehicle.Model} (VIN ending {newVehicle.LastSixVin}):",
                            maxLength: 8,
                            keyboard: Keyboard.Numeric
                        );

                        if (string.IsNullOrEmpty(startMilesInput) || !double.TryParse(startMilesInput, out double startMiles) || startMiles < 0 || startMiles > 999999.99)
                        {
                            Console.WriteLine("CheckVehicleAssignmentAsync: Invalid or cancelled start miles input.");
                            await PageDialogService.DisplayAlertAsync("Error", "Invalid starting miles. Please try again.", "OK");
                            return;
                        }

                        (bool submitSuccess, string submitMessage, int? returnedMileageId) = await authService.SubmitStartMileageAsync(mileageId.Value, startMiles);
                        if (submitSuccess)
                        {
                            Console.WriteLine($"CheckVehicleAssignmentAsync: Successfully submitted start miles for mileage_id={returnedMileageId}, vehicle_id={newVehicle.VehicleId}");
                            await PageDialogService.DisplayAlertAsync("Success", "Vehicle reassigned and starting miles submitted successfully.", "OK");
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
                            WeakReferenceMessenger.Default.Send(new VehicleAssignedMessage(newVehicle));
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
                            await PageDialogService.DisplayAlertAsync("Error", errorMessage, "OK");
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
                                string? startMilesInput = await PageDialogService.DisplayPromptAsync(
                                    "Incomplete Mileage",
                                    $"Please enter the starting miles for vehicle ID {record.VehicleId}:",
                                    maxLength: 8,
                                    keyboard: Keyboard.Numeric
                                );

                                if (string.IsNullOrEmpty(startMilesInput) || !double.TryParse(startMilesInput, out double startMiles) || startMiles < 0 || startMiles > 999999.99)
                                {
                                    Console.WriteLine($"CheckVehicleAssignmentAsync: Invalid start miles input for mileage_id={record.MileageId}.");
                                    await PageDialogService.DisplayAlertAsync("Error", "Invalid starting miles. Please try again.", "OK");
                                    continue;
                                }

                                (bool submitSuccess, string submitMessage, int? returnedMileageId) = await authService.SubmitStartMileageAsync(record.MileageId, startMiles);
                                if (submitSuccess)
                                {
                                    Console.WriteLine($"CheckVehicleAssignmentAsync: Successfully submitted start miles for mileage_id={returnedMileageId}, vehicle_id={record.VehicleId}");
                                    await PageDialogService.DisplayAlertAsync("Success", "Starting miles submitted successfully.", "OK");
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
                                            WeakReferenceMessenger.Default.Send(new VehicleAssignedMessage(updatedVehicle));
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
                                        await PageDialogService.DisplayAlertAsync("Error", errorMessage, "OK");
                                }
                            }
                        }
                    }
                    else
                    {
                        Console.WriteLine($"CheckVehicleAssignmentAsync: Failed to reassign vehicle: {message}");
                        await PageDialogService.DisplayAlertAsync("Error", message, "OK");
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
                await PageDialogService.DisplayAlertAsync("No Vehicle", "No vehicles assigned. Please select a vehicle.", "OK");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"CheckVehicleAssignmentAsync: Error: {ex.Message}, StackTrace: {ex.StackTrace}");
            await PageDialogService.DisplayAlertAsync("Error", "Failed to check vehicle.", "OK");
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