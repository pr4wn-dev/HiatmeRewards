using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using HiatMeApp.Helpers;
using HiatMeApp.Messages;
using HiatMeApp.Models;
using HiatMeApp.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Storage;
using Newtonsoft.Json;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace HiatMeApp.ViewModels;

public partial class VehicleViewModel : BaseViewModel
{
    [ObservableProperty]
    private Vehicle? _vehicle;

    [ObservableProperty]
    private bool _noVehicleMessageVisible = true;

    [ObservableProperty]
    private ObservableCollection<Vehicle> _vehicles = new();
    
    [ObservableProperty]
    private bool _isLoading = true;
    
    [ObservableProperty]
    private bool _isMileageComplete = false;
    
    [ObservableProperty]
    private string _vehicleStatusTitle = "Your Vehicle";
    
    [ObservableProperty]
    private string? _vehicleStatusSubtitle = null;
    
    [ObservableProperty]
    private Color _vehicleHeaderColor = Color.FromArgb("#007BFF");

    public VehicleViewModel() : base()
    {
        Title = "Vehicle";
        Vehicles = new ObservableCollection<Vehicle>();
        IsBusy = false;
        
        // Restore user if null (defensive for standalone app)
        if (App.CurrentUser == null)
        {
            try
            {
                var userDataJson = Preferences.Get("UserData", string.Empty);
                if (!string.IsNullOrEmpty(userDataJson))
                {
                    var storedUser = JsonConvert.DeserializeObject<Models.User>(userDataJson);
                    if (storedUser != null)
                    {
                        App.CurrentUser = storedUser;
                        Console.WriteLine($"VehicleViewModel: Restored user in constructor, Email={storedUser.Email}, VehiclesCount={storedUser.Vehicles?.Count ?? 0}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"VehicleViewModel: Failed to restore user in constructor: {ex.Message}");
            }
        }
        
        Console.WriteLine($"VehicleViewModel: Initialized with Vehicle={(Vehicle != null ? $"VIN ending {Vehicle.LastSixVin}" : "none")}, VehiclesCount={Vehicles.Count}, IsVehicleButtonVisible={IsVehicleButtonVisible}, IsIssuesButtonVisible={IsIssuesButtonVisible}, IsBusy={IsBusy}, CurrentUserId={App.CurrentUser?.UserId ?? 0}");
        
        // Don't load vehicles in constructor - let OnAppearing handle it to avoid timing issues
        Console.WriteLine("VehicleViewModel: Constructor complete, LoadVehicles will be called from OnAppearing");
        
        // Register message handlers with error handling
        try
        {
            WeakReferenceMessenger.Default.Register<VehicleViewModel, VehicleAssignedMessage>(this, (recipient, message) =>
            {
                try
                {
                    if (message?.Value != null)
                    {
                        Console.WriteLine($"VehicleViewModel: Received VehicleAssigned message, VIN ending={message.Value.LastSixVin}, VehicleId={message.Value.VehicleId}, CurrentUserId={message.Value.CurrentUserId}, DateAssigned={message.Value.DateAssigned}");
                        recipient.UpdateVehicle(message.Value);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"VehicleViewModel: Error handling VehicleAssignedMessage: {ex.Message}");
                }
            });

            WeakReferenceMessenger.Default.Register<VehicleViewModel, RefreshVehiclePageMessage>(this, (recipient, message) =>
            {
                try
                {
                    Console.WriteLine($"VehicleViewModel: Received RefreshVehiclePage message, reason={message?.Value ?? "unknown"}");
                    _ = recipient.LoadVehiclesAsync();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"VehicleViewModel: Error handling RefreshVehiclePageMessage: {ex.Message}");
                }
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"VehicleViewModel: Error registering message handlers: {ex.Message}, StackTrace: {ex.StackTrace}");
        }
    }

    public async Task LoadVehiclesAsync()
    {
        try
        {
            IsLoading = true;
            await Task.Delay(150);
            
            // Restore user if needed - App.CurrentUser should already be set by ValidateSessionAsync on app start
            // Only restore from Preferences if App.CurrentUser is null (shouldn't happen if session is valid)
            if (App.CurrentUser == null)
            {
                var userDataJson = Preferences.Get("UserData", string.Empty);
                if (!string.IsNullOrEmpty(userDataJson))
                {
                    try
                    {
                        var storedUser = JsonConvert.DeserializeObject<Models.User>(userDataJson);
                        if (storedUser != null)
                        {
                            App.CurrentUser = storedUser;
                            Console.WriteLine($"LoadVehiclesAsync: Restored user from Preferences, VehiclesCount={storedUser.Vehicles?.Count ?? 0}");
                            // Log mileage records from stored data
                            if (storedUser.Vehicles != null)
                            {
                                foreach (var v in storedUser.Vehicles)
                                {
                                    if (v.MileageRecord != null)
                                    {
                                        Console.WriteLine($"LoadVehiclesAsync: Stored vehicle {v.VehicleId} has MileageRecord - MileageId={v.MileageRecord.MileageId}, StartMiles={v.MileageRecord.StartMiles}, EndingMiles={v.MileageRecord.EndingMiles}");
                                    }
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"LoadVehiclesAsync: Error restoring user from Preferences: {ex.Message}");
                    }
                }
            }
            else
            {
                // Log what we have in App.CurrentUser
                Console.WriteLine($"LoadVehiclesAsync: Using App.CurrentUser, VehiclesCount={App.CurrentUser.Vehicles?.Count ?? 0}");
                if (App.CurrentUser.Vehicles != null)
                {
                    foreach (var v in App.CurrentUser.Vehicles)
                    {
                        if (v.MileageRecord != null)
                        {
                            Console.WriteLine($"LoadVehiclesAsync: App.CurrentUser vehicle {v.VehicleId} has MileageRecord - MileageId={v.MileageRecord.MileageId}, StartMiles={v.MileageRecord.StartMiles}, EndingMiles={v.MileageRecord.EndingMiles}");
                        }
                        else
                        {
                            Console.WriteLine($"LoadVehiclesAsync: App.CurrentUser vehicle {v.VehicleId} has NO MileageRecord");
                        }
                    }
                }
            }
            
            if (App.CurrentUser?.Vehicles != null && App.CurrentUser.UserId > 0)
            {
                var vehiclesList = App.CurrentUser.Vehicles.Where(v => v != null).ToList();
                var userId = App.CurrentUser.UserId;
                var selectedVehicle = vehiclesList
                    .Where(v => v.CurrentUserId == userId)
                    .OrderByDescending(v => DateTime.TryParse(v.DateAssigned, out var date) ? date : DateTime.MinValue)
                    .FirstOrDefault();

                // Log vehicle and mileage record details for debugging
                if (selectedVehicle != null)
                {
                    Console.WriteLine($"LoadVehiclesAsync: Selected vehicle VehicleId={selectedVehicle.VehicleId}, VIN ending={selectedVehicle.LastSixVin}");
                    if (selectedVehicle.MileageRecord != null)
                    {
                        Console.WriteLine($"LoadVehiclesAsync: Vehicle has MileageRecord - MileageId={selectedVehicle.MileageRecord.MileageId}, StartMiles={selectedVehicle.MileageRecord.StartMiles}, EndingMiles={selectedVehicle.MileageRecord.EndingMiles}");
                    }
                    else
                    {
                        Console.WriteLine($"LoadVehiclesAsync: Vehicle has NO MileageRecord");
                    }
                }

                // Update UI on main thread
                await Microsoft.Maui.ApplicationModel.MainThread.InvokeOnMainThreadAsync(() =>
                {
                    Vehicles.Clear();
                    foreach (var v in vehiclesList)
                    {
                        Vehicles.Add(v);
                    }
                    
                    // Set Vehicle property and explicitly notify of changes
                    Vehicle = selectedVehicle;
                    OnPropertyChanged(nameof(Vehicle));
                    if (Vehicle?.MileageRecord != null)
                    {
                        // Force property change notification for nested properties
                        OnPropertyChanged(nameof(Vehicle.MileageRecord));
                        OnPropertyChanged(nameof(Vehicle.MileageRecord.StartMiles));
                        OnPropertyChanged(nameof(Vehicle.MileageRecord.EndingMiles));
                    }
                    
                    // Update vehicle status based on mileage record state
                    UpdateVehicleStatusDisplay();
                    
                    NoVehicleMessageVisible = Vehicle == null;
                    
                    Console.WriteLine($"LoadVehiclesAsync: Set Vehicle property, VehicleId={Vehicle?.VehicleId}, HasMileageRecord={Vehicle?.MileageRecord != null}, StartMiles={Vehicle?.MileageRecord?.StartMiles}");
                });
            }
            else
            {
                await Microsoft.Maui.ApplicationModel.MainThread.InvokeOnMainThreadAsync(() =>
                {
                    Vehicles.Clear();
                    Vehicle = null;
                    NoVehicleMessageVisible = true;
                });
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"LoadVehiclesAsync error: {ex.Message}, {ex.StackTrace}");
            await Microsoft.Maui.ApplicationModel.MainThread.InvokeOnMainThreadAsync(() =>
            {
                Vehicles.Clear();
                Vehicle = null;
                NoVehicleMessageVisible = true;
            });
        }
        finally
        {
            IsLoading = false;
        }
    }
    
    public void LoadVehicles()
    {
        Task.Run(async () => await LoadVehiclesAsync()).Wait();
    }

    private void UpdateVehicle(Vehicle newVehicle)
    {
        try
        {
            if (App.CurrentUser != null && App.CurrentUser.Vehicles != null)
            {
                // Set CurrentUserId if missing
                if (!newVehicle.CurrentUserId.HasValue || newVehicle.CurrentUserId == 0)
                {
                    newVehicle.CurrentUserId = App.CurrentUser.UserId;
                    Console.WriteLine($"UpdateVehicle: Set CurrentUserId={newVehicle.CurrentUserId} for VIN ending={newVehicle.LastSixVin}");
                }
                // Clear CurrentUserId for other vehicles
                var updatedVehicles = App.CurrentUser.Vehicles
                    .Where(v => v.VehicleId != newVehicle.VehicleId)
                    .Select(v => { v.CurrentUserId = null; return v; })
                    .ToList();
                updatedVehicles.Add(newVehicle);
                App.CurrentUser.Vehicles = updatedVehicles;
                Preferences.Set("UserData", JsonConvert.SerializeObject(App.CurrentUser));
                Console.WriteLine($"UpdateVehicle: Updated vehicle list, new vehicle VIN ending={newVehicle.LastSixVin}, VehicleId={newVehicle.VehicleId}, CurrentUserId={newVehicle.CurrentUserId}, DateAssigned={newVehicle.DateAssigned}, Total vehicles={updatedVehicles.Count}, CurrentUserId={App.CurrentUser?.UserId ?? 0}");
                _ = LoadVehiclesAsync(); // Reload to ensure Vehicle property is updated
            }
            else
            {
                Console.WriteLine($"UpdateVehicle: Cannot update - App.CurrentUser is null or has no vehicles");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"UpdateVehicle: Exception occurred: {ex.Message}, StackTrace: {ex.StackTrace}");
        }
    }
    
    private void UpdateVehicleStatusDisplay()
    {
        try
        {
            if (Vehicle == null)
            {
                VehicleStatusTitle = "No Vehicle";
                VehicleStatusSubtitle = null;
                IsMileageComplete = false;
                VehicleHeaderColor = Color.FromArgb("#6c757d"); // Gray
                return;
            }
            
            bool hasMileageRecord = Vehicle.MileageRecord != null;
            bool hasStartMiles = hasMileageRecord && Vehicle.MileageRecord.StartMiles != null;
            bool hasEndMiles = hasMileageRecord && Vehicle.MileageRecord.EndingMiles != null;
            
            IsMileageComplete = hasStartMiles && hasEndMiles;
            
            if (IsMileageComplete)
            {
                // Mileage record is complete - this is a "previous" vehicle (day finished)
                VehicleStatusTitle = "Previous Vehicle";
                VehicleStatusSubtitle = "Day completed - assign to start new record";
                VehicleHeaderColor = Color.FromArgb("#6c757d"); // Gray to indicate inactive
                Console.WriteLine($"UpdateVehicleStatusDisplay: Vehicle {Vehicle.VehicleId} - Mileage complete, showing as Previous Vehicle");
            }
            else if (hasStartMiles && !hasEndMiles)
            {
                // Open mileage record - currently active
                VehicleStatusTitle = "Your Vehicle";
                VehicleStatusSubtitle = "Currently in use";
                VehicleHeaderColor = Color.FromArgb("#28a745"); // Green for active
                Console.WriteLine($"UpdateVehicleStatusDisplay: Vehicle {Vehicle.VehicleId} - Active (open mileage record)");
            }
            else
            {
                // No mileage record or no start miles
                VehicleStatusTitle = "Your Vehicle";
                VehicleStatusSubtitle = "Needs starting miles";
                VehicleHeaderColor = Color.FromArgb("#007BFF"); // Blue default
                Console.WriteLine($"UpdateVehicleStatusDisplay: Vehicle {Vehicle.VehicleId} - Assigned but needs start miles");
            }
            
            OnPropertyChanged(nameof(IsMileageComplete));
            OnPropertyChanged(nameof(VehicleStatusTitle));
            OnPropertyChanged(nameof(VehicleStatusSubtitle));
            OnPropertyChanged(nameof(VehicleHeaderColor));
        }
        catch (Exception ex)
        {
            Console.WriteLine($"UpdateVehicleStatusDisplay: Exception: {ex.Message}");
        }
    }

    private void CheckIncompleteMileageRecord()
    {
        try
        {
            if (Vehicle?.MileageRecord != null && Vehicle.MileageRecord.StartMiles != null && Vehicle.MileageRecord.EndingMiles == null)
            {
                Console.WriteLine($"Incomplete mileage record for vehicle ID {Vehicle.VehicleId}: StartMiles={Vehicle.MileageRecord.StartMiles}, EndingMiles=null");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"CheckIncompleteMileageRecord: Exception: {ex.Message}");
        }
    }

    [RelayCommand]
    private async Task AssignVehicleByVin()
    {
        try
        {
            Console.WriteLine($"AssignVehicleByVin: Starting, IsBusy={IsBusy}, CurrentUserId={App.CurrentUser?.UserId}");
            if (IsBusy) return;
            IsBusy = true;

            // Check if there's a pre-filled VIN suffix from the home page confirmation flow
            string? prefilledVin = Preferences.Get("PrefilledVinSuffix", null);
            string? vinSuffix = null;
            
            if (!string.IsNullOrEmpty(prefilledVin))
            {
                // Clear the prefilled value immediately so it doesn't persist
                Preferences.Remove("PrefilledVinSuffix");
                Console.WriteLine($"AssignVehicleByVin: Found prefilled VIN suffix: {prefilledVin}");
                
                // Confirm with user that they want to continue with this vehicle
                bool confirm = await PageDialogService.DisplayAlertAsync(
                    "Continue with Vehicle",
                    $"You're continuing with your previously assigned vehicle (VIN ending: {prefilledVin}).\n\nTap 'Continue' to start a new mileage record, or 'Change' to use a different vehicle.",
                    "Continue",
                    "Change"
                );
                
                if (confirm)
                {
                    vinSuffix = prefilledVin;
                }
            }
            
            // If no prefilled VIN or user chose to change, prompt for input
            if (string.IsNullOrEmpty(vinSuffix))
            {
                vinSuffix = await PageDialogService.DisplayPromptAsync(
                    "Assign Vehicle",
                    "Enter the last 6 digits of the vehicle's VIN:",
                    maxLength: 6,
                    keyboard: Keyboard.Text
                );

                if (string.IsNullOrEmpty(vinSuffix))
                {
                    Console.WriteLine("AssignVehicleByVin: User cancelled VIN input.");
                    return;
                }
            }

            var authService = App.Services.GetRequiredService<AuthService>();
            
            // Check if user has an incomplete mileage record on their current vehicle
            // They must complete it (enter ending miles) before switching to a new vehicle
            var currentVehicle = App.CurrentUser?.Vehicles?
                .FirstOrDefault(v => v.CurrentUserId == App.CurrentUser.UserId && 
                                     v.MileageRecord != null && 
                                     v.MileageRecord.StartMiles != null && 
                                     v.MileageRecord.EndingMiles == null);
            
            if (currentVehicle != null)
            {
                Console.WriteLine($"AssignVehicleByVin: User has incomplete mileage record on vehicle {currentVehicle.VehicleId} (VIN ending: {currentVehicle.LastSixVin})");
                
                // If they're trying to assign the same vehicle, that's fine - skip the ending miles prompt
                if (currentVehicle.LastSixVin?.ToUpper() == vinSuffix?.ToUpper())
                {
                    Console.WriteLine("AssignVehicleByVin: Same vehicle - no need for ending miles");
                    await PageDialogService.DisplayAlertAsync("Already Assigned", 
                        $"You're already assigned to this vehicle (VIN ending: {currentVehicle.LastSixVin}) with an active mileage record.", "OK");
                    return;
                }
                
                // Prompt for ending miles on current vehicle before switching
                bool wantToFinish = await PageDialogService.DisplayAlertAsync(
                    "Complete Current Record",
                    $"You have an open mileage record on your current vehicle ({currentVehicle.Make} {currentVehicle.Model}, VIN ending: {currentVehicle.LastSixVin}).\n\nYou must enter ending miles before switching to a new vehicle.",
                    "Enter Ending Miles",
                    "Cancel"
                );
                
                if (!wantToFinish)
                {
                    Console.WriteLine("AssignVehicleByVin: User cancelled - needs to complete current mileage first");
                    return;
                }
                
                // Prompt for ending miles
                string? endMilesInput = await PageDialogService.DisplayPromptAsync(
                    "Ending Miles",
                    $"Enter the ending miles for {currentVehicle.Make} {currentVehicle.Model}:",
                    maxLength: 8,
                    keyboard: Keyboard.Numeric
                );
                
                if (string.IsNullOrEmpty(endMilesInput) || !double.TryParse(endMilesInput, out double endMiles) || endMiles < 0 || endMiles > 999999.99)
                {
                    Console.WriteLine("AssignVehicleByVin: Invalid ending miles input");
                    await PageDialogService.DisplayAlertAsync("Error", "Invalid ending miles. Please try again.", "OK");
                    return;
                }
                
                // Validate ending miles is greater than start miles
                if (currentVehicle.MileageRecord?.StartMiles != null && endMiles <= currentVehicle.MileageRecord.StartMiles)
                {
                    Console.WriteLine($"AssignVehicleByVin: Ending miles ({endMiles}) must be greater than start miles ({currentVehicle.MileageRecord.StartMiles})");
                    await PageDialogService.DisplayAlertAsync("Error", 
                        $"Ending miles ({endMiles}) must be greater than starting miles ({currentVehicle.MileageRecord.StartMiles}).", "OK");
                    return;
                }
                
                // Submit ending miles for current vehicle
                Console.WriteLine($"AssignVehicleByVin: Submitting ending miles {endMiles} for vehicle {currentVehicle.VehicleId}");
                var (endSuccess, endMessage, _) = await authService.SubmitEndMileageAsync(currentVehicle.VehicleId, endMiles);
                
                if (!endSuccess)
                {
                    Console.WriteLine($"AssignVehicleByVin: Failed to submit ending miles: {endMessage}");
                    await PageDialogService.DisplayAlertAsync("Error", $"Failed to submit ending miles: {endMessage}", "OK");
                    return;
                }
                
                Console.WriteLine("AssignVehicleByVin: Successfully submitted ending miles, proceeding with new vehicle assignment");
                await PageDialogService.DisplayAlertAsync("Success", "Ending miles recorded. Now assigning new vehicle...", "OK");
                
                // Update local data
                if (currentVehicle.MileageRecord != null)
                {
                    currentVehicle.MileageRecord.EndingMiles = (float)endMiles;
                    currentVehicle.MileageRecord.EndingMilesDatetime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                }
                Preferences.Set("UserData", JsonConvert.SerializeObject(App.CurrentUser));
            }
            
            // Log the auth token we're about to use
            var currentAuthToken = Preferences.Get("AuthToken", null);
            Console.WriteLine($"AssignVehicleByVin: Current AuthToken from Preferences={(string.IsNullOrEmpty(currentAuthToken) ? "NULL" : currentAuthToken.Substring(0, Math.Min(20, currentAuthToken.Length)) + "...")}");
            
            // Validate session first to ensure auth token is still valid on the server
            Console.WriteLine("AssignVehicleByVin: Validating session before assignment");
            var (sessionValid, validatedUser, sessionMessage) = await authService.ValidateSessionAsync();
            
            if (!sessionValid)
            {
                Console.WriteLine($"AssignVehicleByVin: Session validation failed - {sessionMessage}");
                await PageDialogService.DisplayAlertAsync("Session Expired", "Your session has expired. Please log in again.", "OK");
                Preferences.Set("IsLoggedIn", false);
                await Shell.Current.GoToAsync("//Login");
                return;
            }
            
            Console.WriteLine("AssignVehicleByVin: Session validated successfully");
            
            // Update App.CurrentUser with fresh data from server
            if (validatedUser != null)
            {
                App.CurrentUser = validatedUser;
                Preferences.Set("UserData", JsonConvert.SerializeObject(validatedUser));
                Console.WriteLine($"AssignVehicleByVin: Updated App.CurrentUser from server, VehiclesCount={validatedUser.Vehicles?.Count ?? 0}");
            }
            
            // After validation, the CSRF token should be fresh - no need to fetch again
            // ValidateSessionAsync updates _csrfToken from the response
            
            // Log the auth token after validation (should be the same)
            var postValidationToken = Preferences.Get("AuthToken", null);
            Console.WriteLine($"AssignVehicleByVin: AuthToken after validation={(string.IsNullOrEmpty(postValidationToken) ? "NULL" : postValidationToken.Substring(0, Math.Min(20, postValidationToken.Length)) + "...")}");
            
            bool assignmentSuccessful = false;

            while (!assignmentSuccessful)
            {
                // Use allowIncompleteEndingMiles: true to allow creating new mileage record when previous is complete
                (bool success, Vehicle? newVehicle, string message, List<MileageRecord>? incompleteRecords, int? mileageId) = await authService.AssignVehicleAsync(vinSuffix, allowIncompleteEndingMiles: true);

                if (success && newVehicle != null && mileageId.HasValue)
                {
                    UpdateVehicle(newVehicle);
                    Console.WriteLine($"AssignVehicleByVin: Assigned new vehicle, VIN={newVehicle.Vin}, VehicleId={newVehicle.VehicleId}, CurrentUserId={newVehicle.CurrentUserId}, DateAssigned={newVehicle.DateAssigned}, MileageId={mileageId}");

                    string? startMilesInput = await PageDialogService.DisplayPromptAsync(
                        "New Mileage Entry",
                        $"Please enter the starting miles for {newVehicle.Make} {newVehicle.Model} (VIN ending {newVehicle.LastSixVin}):",
                        maxLength: 8,
                        keyboard: Keyboard.Numeric
                    );

                    if (string.IsNullOrEmpty(startMilesInput) || !double.TryParse(startMilesInput, out double startMiles) || startMiles < 0 || startMiles > 999999.99)
                    {
                        Console.WriteLine("AssignVehicleByVin: Invalid or cancelled start miles input.");
                        await PageDialogService.DisplayAlertAsync("Error", "Invalid starting miles. Please try again.", "OK");
                        break;
                    }

                    (bool submitSuccess, string submitMessage, int? returnedMileageId) = await authService.SubmitStartMileageAsync(mileageId.Value, startMiles);
                    if (submitSuccess)
                    {
                        Console.WriteLine($"AssignVehicleByVin: Successfully submitted start miles for mileage_id={returnedMileageId}, vehicle_id={newVehicle.VehicleId}");
                        await PageDialogService.DisplayAlertAsync("Success", "Vehicle assigned and starting miles submitted successfully.", "OK");
                        newVehicle.MileageRecord = new MileageRecord
                        {
                            MileageId = returnedMileageId ?? 0,
                            VehicleId = newVehicle.VehicleId,
                            UserId = App.CurrentUser?.UserId ?? 0,
                            StartMiles = (float?)startMiles,
                            StartMilesDatetime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
                        };
                        UpdateVehicle(newVehicle);
                        WeakReferenceMessenger.Default.Send(new VehicleAssignedMessage(newVehicle));
                        assignmentSuccessful = true;
                        // Send refresh message - no need to navigate since we're already on the Vehicle page
                        WeakReferenceMessenger.Default.Send(new RefreshVehiclePageMessage("force"));
                        // Just reload the vehicles to refresh the UI
                        await LoadVehiclesAsync();
                    }
                    else
                    {
                        Console.WriteLine($"AssignVehicleByVin: Failed to submit start miles: {submitMessage}");
                        // Don't show another popup if it's a "logged in elsewhere" error - that popup was already shown
                        if (!submitMessage.StartsWith("LOGGED_IN_ELSEWHERE:", StringComparison.OrdinalIgnoreCase))
                        {
                            string errorMessage = submitMessage;
                            if (submitMessage.Contains("Too many attempts"))
                            {
                                errorMessage = "Rate limit exceeded. Please wait 15 minutes and try again.";
                            }
                            else if (submitMessage.Contains("Network error"))
                            {
                                errorMessage = "Network issue submitting mileage. Please check your connection and try again.";
                            }
                            else if (submitMessage.Contains("Invalid mileage ID") || submitMessage.Contains("Mileage record not owned"))
                            {
                                errorMessage = "Failed to submit starting miles. Please try assigning the vehicle again.";
                            }
                            await PageDialogService.DisplayAlertAsync("Error", errorMessage, "OK");
                        }
                        break;
                    }
                }
                else if (incompleteRecords != null && incompleteRecords.Any())
                {
                    foreach (var record in incompleteRecords)
                    {
                        bool needsStartMiles = record.MissingFields?.Contains("start_miles") == true || record.MissingFields?.Contains("start_miles_datetime") == true;
                        bool needsEndMiles = record.MissingFields?.Contains("ending_miles") == true || record.MissingFields?.Contains("ending_miles_datetime") == true;

                        if (needsStartMiles)
                        {
                            var vehicle = App.CurrentUser?.Vehicles?.FirstOrDefault(v => v.VehicleId == record.VehicleId);
                            string vehicleDescription = vehicle != null ? $"{vehicle.Make} {vehicle.Model} (VIN ending {vehicle.LastSixVin})" : $"vehicle ID {record.VehicleId}";

                            string? startMilesInput = await PageDialogService.DisplayPromptAsync(
                                "Incomplete Mileage",
                                $"Please enter the starting miles for {vehicleDescription}:",
                                maxLength: 8,
                                keyboard: Keyboard.Numeric
                            );

                            if (string.IsNullOrEmpty(startMilesInput) || !double.TryParse(startMilesInput, out double startMiles) || startMiles < 0 || startMiles > 999999.99)
                            {
                                Console.WriteLine("AssignVehicleByVin: Invalid or cancelled start miles input.");
                                await PageDialogService.DisplayAlertAsync("Error", "Invalid starting miles. Please try again.", "OK");
                                continue;
                            }

                            (bool submitSuccess, string submitMessage, int? returnedMileageId) = await authService.SubmitStartMileageAsync(record.MileageId, startMiles);
                            if (!submitSuccess)
                            {
                                Console.WriteLine($"AssignVehicleByVin: Failed to submit start miles: {submitMessage}");
                                // Don't show another popup if it's a "logged in elsewhere" error - that popup was already shown
                                if (!submitMessage.StartsWith("LOGGED_IN_ELSEWHERE:", StringComparison.OrdinalIgnoreCase))
                                {
                                    string errorMessage = submitMessage;
                                    if (submitMessage.Contains("Too many attempts"))
                                    {
                                        errorMessage = "Rate limit exceeded. Please wait 15 minutes and try again.";
                                    }
                                    else if (submitMessage.Contains("Network error"))
                                    {
                                        errorMessage = "Network issue submitting mileage. Please check your connection and try again.";
                                    }
                                    await PageDialogService.DisplayAlertAsync("Error", errorMessage, "OK");
                                }
                                continue;
                            }
                            Console.WriteLine($"AssignVehicleByVin: Submitted start miles for mileage_id={returnedMileageId}, vehicle_id={record.VehicleId}");
                        }

                        if (needsEndMiles)
                        {
                            var vehicle = App.CurrentUser?.Vehicles?.FirstOrDefault(v => v.VehicleId == record.VehicleId);
                            string vehicleDescription = vehicle != null ? $"{vehicle.Make} {vehicle.Model} (VIN ending {vehicle.LastSixVin})" : $"vehicle ID {record.VehicleId}";

                            string? endMilesInput = await PageDialogService.DisplayPromptAsync(
                                "Incomplete Mileage",
                                $"Please enter the ending miles for {vehicleDescription}:",
                                maxLength: 8,
                                keyboard: Keyboard.Numeric
                            );

                            if (string.IsNullOrEmpty(endMilesInput) || !double.TryParse(endMilesInput, out double endMiles) || endMiles < 0 || endMiles > 999999.99)
                            {
                                Console.WriteLine("AssignVehicleByVin: Invalid or cancelled end miles input.");
                                await PageDialogService.DisplayAlertAsync("Error", "Invalid ending miles. Please try again.", "OK");
                                continue;
                            }

                            (bool submitSuccess, string submitMessage, int? returnedMileageId) = await authService.SubmitEndMileageAsync(record.VehicleId, endMiles);
                            if (!submitSuccess)
                            {
                                Console.WriteLine($"AssignVehicleByVin: Failed to submit end miles: {submitMessage}");
                                // Don't show another popup if it's a "logged in elsewhere" error - that popup was already shown
                                if (!submitMessage.StartsWith("LOGGED_IN_ELSEWHERE:", StringComparison.OrdinalIgnoreCase))
                                {
                                    string errorMessage = submitMessage;
                                    if (submitMessage.Contains("Too many attempts"))
                                    {
                                        errorMessage = "Rate limit exceeded. Please wait 15 minutes and try again.";
                                    }
                                    else if (submitMessage.Contains("Network error"))
                                    {
                                        errorMessage = "Network issue submitting mileage. Please check your connection and try again.";
                                    }
                                    await PageDialogService.DisplayAlertAsync("Error", errorMessage, "OK");
                                }
                                continue;
                            }
                            Console.WriteLine($"AssignVehicleByVin: Submitted end miles for mileage_id={returnedMileageId}, vehicle_id={record.VehicleId}");
                        }
                    }
                    (success, newVehicle, message, incompleteRecords, mileageId) = await authService.AssignVehicleAsync(vinSuffix, allowIncompleteEndingMiles: true);
                    if (success && newVehicle != null && mileageId.HasValue)
                    {
                        UpdateVehicle(newVehicle);
                        Console.WriteLine($"AssignVehicleByVin: Assigned new vehicle after resolving incomplete records, VIN={newVehicle.Vin}, VehicleId={newVehicle.VehicleId}, CurrentUserId={newVehicle.CurrentUserId}, DateAssigned={newVehicle.DateAssigned}, MileageId={mileageId}");

                        string? startMilesInput = await PageDialogService.DisplayPromptAsync(
                            "New Mileage Entry",
                            $"Please enter the starting miles for {newVehicle.Make} {newVehicle.Model} (VIN ending {newVehicle.LastSixVin}):",
                            maxLength: 8,
                            keyboard: Keyboard.Numeric
                        );

                        if (string.IsNullOrEmpty(startMilesInput) || !double.TryParse(startMilesInput, out double startMiles) || startMiles < 0 || startMiles > 999999.99)
                        {
                            Console.WriteLine("AssignVehicleByVin: Invalid or cancelled start miles input.");
                            await PageDialogService.DisplayAlertAsync("Error", "Invalid starting miles. Please try again.", "OK");
                            break;
                        }

                        (bool submitSuccess, string submitMessage, int? returnedMileageId) = await authService.SubmitStartMileageAsync(mileageId.Value, startMiles);
                        if (submitSuccess)
                        {
                            Console.WriteLine($"AssignVehicleByVin: Successfully submitted start miles for mileage_id={returnedMileageId}, vehicle_id={newVehicle.VehicleId}");
                            await PageDialogService.DisplayAlertAsync("Success", "Vehicle assigned and starting miles submitted successfully.", "OK");
                            newVehicle.MileageRecord = new MileageRecord
                            {
                                MileageId = returnedMileageId ?? 0,
                                VehicleId = newVehicle.VehicleId,
                                UserId = App.CurrentUser?.UserId ?? 0,
                                StartMiles = (float?)startMiles,
                                StartMilesDatetime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
                            };
                            UpdateVehicle(newVehicle);
                        WeakReferenceMessenger.Default.Send(new VehicleAssignedMessage(newVehicle));
                            assignmentSuccessful = true;
                            // Send refresh message
                        WeakReferenceMessenger.Default.Send(new RefreshVehiclePageMessage("force"));
                            await Shell.Current.GoToAsync($"Vehicle?refresh={Guid.NewGuid()}");
                        }
                        else
                        {
                            Console.WriteLine($"AssignVehicleByVin: Failed to submit start miles: {submitMessage}");
                            // Don't show another popup if it's a "logged in elsewhere" error - that popup was already shown
                            if (!submitMessage.StartsWith("LOGGED_IN_ELSEWHERE:", StringComparison.OrdinalIgnoreCase))
                            {
                                string errorMessage = submitMessage;
                                if (submitMessage.Contains("Too many attempts"))
                                {
                                    errorMessage = "Rate limit exceeded. Please wait 15 minutes and try again.";
                                }
                                else if (submitMessage.Contains("Network error"))
                                {
                                    errorMessage = "Network issue submitting mileage. Please check your connection and try again.";
                                }
                                else if (submitMessage.Contains("Invalid mileage ID") || submitMessage.Contains("Mileage record not owned"))
                                {
                                    errorMessage = "Failed to submit starting miles. Please try assigning the vehicle again.";
                                }
                                await PageDialogService.DisplayAlertAsync("Error", errorMessage, "OK");
                            }
                            break;
                        }
                    }
                    else
                    {
                        Console.WriteLine($"AssignVehicleByVin: Failed to assign vehicle after resolving incomplete records: {message}");
                        // Don't show another popup if it's a "logged in elsewhere" error - that popup was already shown
                        if (!message.StartsWith("LOGGED_IN_ELSEWHERE:", StringComparison.OrdinalIgnoreCase))
                        {
                            string errorMessage = message;
                            if (message.Contains("Network error"))
                            {
                                errorMessage = "Network issue assigning vehicle. Please check your connection and try again.";
                            }
                            await PageDialogService.DisplayAlertAsync("Error", errorMessage, "OK");
                        }
                        break;
                    }
                }
                else
                {
                    Console.WriteLine($"AssignVehicleByVin: Failed to assign vehicle: {message}");
                    // Don't show another popup if it's a "logged in elsewhere" error - that popup was already shown
                    if (!message.StartsWith("LOGGED_IN_ELSEWHERE:", StringComparison.OrdinalIgnoreCase))
                    {
                        string errorMessage = message;
                        if (message.Contains("Network error"))
                        {
                            errorMessage = "Network issue assigning vehicle. Please check your connection and try again.";
                        }
                        await PageDialogService.DisplayAlertAsync("Error", errorMessage, "OK");
                    }
                    break;
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"AssignVehicleByVin: Error: {ex.Message}, StackTrace: {ex.StackTrace}");
            await PageDialogService.DisplayAlertAsync("Error", "Failed to assign vehicle.", "OK");
        }
        finally
        {
            IsBusy = false;
            Console.WriteLine($"AssignVehicleByVin: Set IsBusy=false");
        }
    }

    [RelayCommand]
    private async Task SubmitEndMileage()
    {
        try
        {
            if (IsBusy || Vehicle == null || Vehicle.MileageRecord == null || Vehicle.MileageRecord.StartMiles == null || Vehicle.MileageRecord.EndingMiles != null)
            {
                Console.WriteLine($"SubmitEndMileage: Cannot submit. IsBusy={IsBusy}, Vehicle={(Vehicle != null ? $"VIN ending {Vehicle.LastSixVin}" : "null")}, MileageRecord={(Vehicle?.MileageRecord != null ? $"MileageId={Vehicle.MileageRecord.MileageId}" : "null")}");
                await PageDialogService.DisplayAlertAsync("Error", "No valid mileage record to submit ending miles for.", "OK");
                return;
            }

            IsBusy = true;
            string? endMilesInput = await PageDialogService.DisplayPromptAsync(
                "End Mileage",
                $"Please enter the ending miles for {Vehicle.Make} {Vehicle.Model} (VIN ending {Vehicle.LastSixVin}):",
                maxLength: 8,
                keyboard: Keyboard.Numeric
            );

            if (string.IsNullOrEmpty(endMilesInput) || !double.TryParse(endMilesInput, out double endMiles) || endMiles < Vehicle.MileageRecord.StartMiles || endMiles > 999999.99)
            {
                Console.WriteLine("SubmitEndMileage: Invalid or cancelled end miles input.");
                await PageDialogService.DisplayAlertAsync("Error", "Invalid ending miles. Must be greater than or equal to starting miles.", "OK");
                return;
            }

            var authService = App.Services.GetRequiredService<AuthService>();
            (bool submitSuccess, string submitMessage, int? returnedMileageId) = await authService.SubmitEndMileageAsync(Vehicle.VehicleId, endMiles);

            if (submitSuccess)
            {
                Console.WriteLine($"SubmitEndMileage: Successfully submitted end miles for mileage_id={returnedMileageId}, vehicle_id={Vehicle.VehicleId}");
                await PageDialogService.DisplayAlertAsync("Success", "Ending miles submitted successfully.", "OK");
                Vehicle.MileageRecord.EndingMiles = (float?)endMiles;
                Vehicle.MileageRecord.EndingMilesDatetime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                UpdateVehicle(Vehicle);
                WeakReferenceMessenger.Default.Send(new VehicleAssignedMessage(Vehicle));
                // Send refresh message
                WeakReferenceMessenger.Default.Send(new RefreshVehiclePageMessage("force"));
                await Shell.Current.GoToAsync($"Vehicle?refresh={Guid.NewGuid()}");
            }
            else
            {
                Console.WriteLine($"SubmitEndMileage: Failed to submit end miles: {submitMessage}");
                // Don't show another popup if it's a "logged in elsewhere" error - that popup was already shown
                if (!submitMessage.StartsWith("LOGGED_IN_ELSEWHERE:", StringComparison.OrdinalIgnoreCase))
                {
                    string errorMessage = submitMessage;
                    if (submitMessage.Contains("Too many attempts"))
                    {
                        errorMessage = "Rate limit exceeded. Please wait 15 minutes and try again.";
                    }
                    else if (submitMessage.Contains("Network error"))
                    {
                        errorMessage = "Network issue submitting mileage. Please check your connection and try again.";
                    }
                    await PageDialogService.DisplayAlertAsync("Error", errorMessage, "OK");
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"SubmitEndMileage: Error: {ex.Message}, StackTrace: {ex.StackTrace}");
            await PageDialogService.DisplayAlertAsync("Error", "Failed to submit ending miles.", "OK");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task ReportCameraIssue()
    {
        try
        {
            if (Vehicle == null)
            {
                await PageDialogService.DisplayAlertAsync("No Vehicle", "Please assign a vehicle first.", "OK");
                return;
            }

            Console.WriteLine("ReportCameraIssue: Showing camera issue type selection");
            string[] cameraOptions = { "Camera Issue", "SD Card Full" };
            string? selectedType = await PageDialogService.DisplayActionSheetAsync("Camera/Chip Issue", "Cancel", null, cameraOptions);

            if (string.IsNullOrEmpty(selectedType) || selectedType == "Cancel")
            {
                Console.WriteLine("ReportCameraIssue: User cancelled issue type selection");
                return;
            }

            string? description = await PageDialogService.DisplayPromptAsync(
                "Describe Issue",
                selectedType == "SD Card Full" 
                    ? "Any additional details about the SD card? (optional)" 
                    : "Describe the camera issue (optional):",
                maxLength: 500,
                keyboard: Keyboard.Text
            );

            if (description == null)
            {
                Console.WriteLine("ReportCameraIssue: User cancelled description input");
                return;
            }

            IsBusy = true;
            var authService = App.Services.GetRequiredService<AuthService>();
            var (success, message) = await authService.AddVehicleIssueAsync(Vehicle.VehicleId, selectedType, description);

            if (success)
            {
                Console.WriteLine($"ReportCameraIssue: Successfully reported {selectedType} for vehicle {Vehicle.VehicleId}");
                await PageDialogService.DisplayAlertAsync("Success", $"{selectedType} reported successfully. A manager will be notified.", "OK");
            }
            else
            {
                Console.WriteLine($"ReportCameraIssue: Failed to report issue: {message}");
                if (!message.StartsWith("LOGGED_IN_ELSEWHERE:", StringComparison.OrdinalIgnoreCase))
                {
                    await PageDialogService.DisplayAlertAsync("Error", message, "OK");
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"ReportCameraIssue: Error: {ex.Message}");
            await PageDialogService.DisplayAlertAsync("Error", "Failed to report camera issue.", "OK");
        }
        finally
        {
            IsBusy = false;
        }
    }
}