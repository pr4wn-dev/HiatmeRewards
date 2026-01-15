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
    private Vehicle? _assignedVehicle;

    [ObservableProperty]
    private string? _userName;

    [ObservableProperty]
    private string? _userRole;

    [ObservableProperty]
    private string? _vehicleStatus;

    [ObservableProperty]
    private string _currentDate = DateTime.Now.ToString("dddd, MMMM d");

    public HomeViewModel() : base()
    {
        Title = "Home";
        var userJson = Preferences.Get("UserData", string.Empty);
        if (!string.IsNullOrEmpty(userJson))
        {
            var user = JsonConvert.DeserializeObject<User>(userJson);
            App.CurrentUser = user;
            UserName = user?.Name ?? "User";
            UserRole = user?.Role ?? "User";
            UpdateVehicleStatus();
            Console.WriteLine($"HomeViewModel: Initialized with IsVehicleButtonVisible={IsVehicleButtonVisible}, IsClient={IsClient}, Role={user?.Role}, UserId={user?.UserId}, VehiclesCount={user?.Vehicles?.Count ?? 0}");
        }
        
        // Only register for vehicle messages and check assignments if not a Client
        if (!IsClient)
        {
            WeakReferenceMessenger.Default.Register<HomeViewModel, VehicleAssignedMessage>(this, (recipient, message) =>
            {
                Console.WriteLine($"HomeViewModel: Received VehicleAssigned message, VIN ending={message.Value.LastSixVin}");
                recipient.AssignedVehicle = message.Value;
                recipient.OnPropertyChanged(nameof(AssignedVehicle));
            });
            CheckVehicleAssignmentAsync();
        }
        else
        {
            Console.WriteLine("HomeViewModel: Client role detected - skipping vehicle assignment check");
        }
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

                // Only show vehicle confirmation if:
                // 1. We just logged in (ShouldConfirmVehicle flag is set)
                // 2. The mileage record is COMPLETE (has both start AND ending miles)
                // This means they finished their day and we need to ask if they want to continue with same vehicle
                bool shouldConfirm = Preferences.Get("ShouldConfirmVehicle", false);
                bool hasMileageRecord = AssignedVehicle.MileageRecord != null;
                bool mileageIsComplete = hasMileageRecord && 
                    AssignedVehicle.MileageRecord.StartMiles != null && 
                    AssignedVehicle.MileageRecord.EndingMiles != null;
                bool hasOpenMileageRecord = hasMileageRecord && 
                    AssignedVehicle.MileageRecord.StartMiles != null && 
                    AssignedVehicle.MileageRecord.EndingMiles == null;
                
                Console.WriteLine($"CheckVehicleAssignmentAsync: shouldConfirm={shouldConfirm}, hasMileageRecord={hasMileageRecord}, mileageIsComplete={mileageIsComplete}, hasOpenMileageRecord={hasOpenMileageRecord}");
                
                // Clear the flag now - we'll handle confirmation logic below
                if (shouldConfirm)
                {
                    Preferences.Set("ShouldConfirmVehicle", false);
                }
                
                // If there's an open mileage record (start miles but no end miles), driver is still in vehicle - no confirmation needed
                if (hasOpenMileageRecord)
                {
                    Console.WriteLine($"CheckVehicleAssignmentAsync: Open mileage record found for vehicle_id={AssignedVehicle.VehicleId}, mileage_id={AssignedVehicle.MileageRecord.MileageId} - no confirmation needed");
                    return;
                }
                
                // Only ask for confirmation if mileage record is complete (they finished their day)
                if (shouldConfirm && mileageIsComplete)
                {
                    string vinSuffix = AssignedVehicle.LastSixVin ?? string.Empty;
                    bool isStillInVehicle = await PageDialogService.DisplayAlertAsync(
                        "Vehicle Confirmation",
                        $"Is this your current vehicle: {AssignedVehicle.Make} {AssignedVehicle.Model} (VIN ending {vinSuffix})?",
                        "Yes",
                        "No"
                    );

                    if (!isStillInVehicle)
                    {
                        Console.WriteLine("CheckVehicleAssignmentAsync: User denied vehicle confirmation");
                        
                        // Remove this vehicle from current user assignment
                        var updatedVehicles = App.CurrentUser.Vehicles
                            .Where(v => !(v.VehicleId == AssignedVehicle.VehicleId && v.CurrentUserId == App.CurrentUser.UserId))
                            .ToList();
                        App.CurrentUser.Vehicles = updatedVehicles;
                        
                        AssignedVehicle = null;
                        OnPropertyChanged(nameof(AssignedVehicle));
                        Preferences.Set("UserData", JsonConvert.SerializeObject(App.CurrentUser));
                        Console.WriteLine("CheckVehicleAssignmentAsync: Vehicle assignment cleared, navigating to Vehicle page.");
                        await Shell.Current.GoToAsync("Vehicle");
                        return;
                    }
                    
                    // User clicked "Yes" - navigate to Vehicle page with VIN pre-filled
                    // This ensures proper authentication flow through the VehicleViewModel
                    Console.WriteLine($"CheckVehicleAssignmentAsync: User confirmed same vehicle, navigating to Vehicle page with VIN suffix: {vinSuffix}");
                    if (!string.IsNullOrEmpty(vinSuffix))
                    {
                        // Pass the VIN suffix to auto-fill on the Vehicle page
                        Preferences.Set("PrefilledVinSuffix", vinSuffix);
                    }
                    await Shell.Current.GoToAsync("Vehicle");
                    return;
                }

                // If mileage is complete but we're not in the confirmation flow (shouldConfirm was false),
                // just stay on Home page. User can navigate to Vehicle page manually to see "Previous Vehicle" status.
                // Don't auto-navigate or auto-assign - only do that when user explicitly confirms after login.
                if (AssignedVehicle.MileageRecord != null && 
                    AssignedVehicle.MileageRecord.StartMiles != null && 
                    AssignedVehicle.MileageRecord.EndingMiles != null)
                {
                    Console.WriteLine($"CheckVehicleAssignmentAsync: Mileage record is complete for vehicle_id={AssignedVehicle.VehicleId}, staying on Home page (no auto-navigate)");
                    // Just stay on home - user finished their day, they can navigate manually if needed
                    return;
                }

                // Note: Open mileage record check (start miles but no end miles) is now handled earlier in the flow
                
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
                        // Don't show another popup if it's a "logged in elsewhere" error - that popup was already shown
                        if (!submitMessage.StartsWith("LOGGED_IN_ELSEWHERE:", StringComparison.OrdinalIgnoreCase))
                        {
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
                            // Don't show another popup if it's a "logged in elsewhere" error - that popup was already shown
                            if (!submitMessage.StartsWith("LOGGED_IN_ELSEWHERE:", StringComparison.OrdinalIgnoreCase))
                            {
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
                                    // Don't show another popup if it's a "logged in elsewhere" error - that popup was already shown
                                    if (!submitMessage.StartsWith("LOGGED_IN_ELSEWHERE:", StringComparison.OrdinalIgnoreCase))
                                    {
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
                    }
                    else
                    {
                        Console.WriteLine($"CheckVehicleAssignmentAsync: Failed to reassign vehicle: {message}");
                        // Don't show another popup if it's a "logged in elsewhere" error - that popup was already shown
                        if (!message.StartsWith("LOGGED_IN_ELSEWHERE:", StringComparison.OrdinalIgnoreCase))
                        {
                            await PageDialogService.DisplayAlertAsync("Error", message, "OK");
                            await Shell.Current.GoToAsync("Vehicle");
                        }
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

    private void UpdateVehicleStatus()
    {
        if (App.CurrentUser?.Vehicles != null && App.CurrentUser.Vehicles.Any())
        {
            var assigned = App.CurrentUser.Vehicles
                .FirstOrDefault(v => v.CurrentUserId == App.CurrentUser.UserId);
            if (assigned != null)
            {
                VehicleStatus = $"{assigned.Make} {assigned.Model}";
            }
            else
            {
                VehicleStatus = "Not Assigned";
            }
        }
        else
        {
            VehicleStatus = "Not Assigned";
        }
    }
}