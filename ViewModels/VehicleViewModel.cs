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
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace HiatMeApp.ViewModels;

public partial class VehicleViewModel : BaseViewModel
{
    [ObservableProperty]
    private Vehicle? _vehicle;

    [ObservableProperty]
    private bool _noVehicleMessageVisible;

    [ObservableProperty]
    private bool _isVehicleButtonVisible;

    [ObservableProperty]
    private ObservableCollection<Vehicle> _vehicles;

    public VehicleViewModel()
    {
        Title = "Vehicle";
        Vehicles = new ObservableCollection<Vehicle>();
        IsBusy = false;
        IsVehicleButtonVisible = App.CurrentUser?.Role is "Driver" or "Manager" or "Owner";
        Console.WriteLine($"VehicleViewModel: Initialized with Vehicle={(Vehicle != null ? $"VIN ending {Vehicle.LastSixVin}" : "none")}, VehiclesCount={Vehicles.Count}, IsVehicleButtonVisible={IsVehicleButtonVisible}, IsBusy={IsBusy}, CurrentUserId={App.CurrentUser?.UserId}");
        LoadVehicles();
        CheckIncompleteMileageRecord();
        WeakReferenceMessenger.Default.Register<VehicleViewModel, VehicleAssignedMessage>(this, (recipient, message) =>
        {
            Console.WriteLine($"VehicleViewModel: Received VehicleAssigned message, VIN ending={message.Value.LastSixVin}, VehicleId={message.Value.VehicleId}, CurrentUserId={message.Value.CurrentUserId}, DateAssigned={message.Value.DateAssigned}");
            recipient.UpdateVehicle(message.Value);
        });

        WeakReferenceMessenger.Default.Register<VehicleViewModel, RefreshVehiclePageMessage>(this, (recipient, message) =>
        {
            Console.WriteLine($"VehicleViewModel: Received RefreshVehiclePage message, reason={message.Value}");
            recipient.LoadVehicles();
        });
    }

    public void LoadVehicles()
    {
        if (App.CurrentUser != null && App.CurrentUser.Vehicles != null && App.CurrentUser.UserId > 0)
        {
            Vehicles.Clear();
            foreach (var vehicle in App.CurrentUser.Vehicles)
            {
                Vehicles.Add(vehicle);
                Console.WriteLine($"LoadVehicles: Vehicle VIN ending={vehicle.LastSixVin}, VehicleId={vehicle.VehicleId}, CurrentUserId={vehicle.CurrentUserId}, DateAssigned={vehicle.DateAssigned}");
            }
            // Select vehicle with matching CurrentUserId only
            var selectedVehicle = Vehicles
                .Where(v => v.CurrentUserId == (App.CurrentUser?.UserId ?? 0))
                .OrderByDescending(v => DateTime.TryParse(v.DateAssigned, out var date) ? date : DateTime.MinValue)
                .FirstOrDefault();

            Vehicle = selectedVehicle;
            NoVehicleMessageVisible = Vehicle == null;
            Console.WriteLine($"LoadVehicles: Loaded {Vehicles.Count} vehicles, Selected Vehicle={(Vehicle != null ? $"VIN ending {Vehicle.LastSixVin}, VehicleId={Vehicle.VehicleId}, CurrentUserId={Vehicle.CurrentUserId}, DateAssigned={Vehicle.DateAssigned}" : "none")}, CurrentUserId={App.CurrentUser?.UserId ?? 0}");
        }
        else
        {
            Vehicles.Clear();
            Vehicle = null;
            NoVehicleMessageVisible = true;
            Console.WriteLine($"LoadVehicles: No vehicles or user data available, CurrentUserId={App.CurrentUser?.UserId}");
        }
        OnPropertyChanged(nameof(Vehicles));
        OnPropertyChanged(nameof(Vehicle));
        OnPropertyChanged(nameof(NoVehicleMessageVisible));
    }

    private void UpdateVehicle(Vehicle newVehicle)
    {
        if (App.CurrentUser?.Vehicles != null)
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
            LoadVehicles(); // Reload to ensure Vehicle property is updated
        }
    }

    private void CheckIncompleteMileageRecord()
    {
        if (Vehicle?.MileageRecord != null && Vehicle.MileageRecord.StartMiles != null && Vehicle.MileageRecord.EndingMiles == null)
        {
            Console.WriteLine($"Incomplete mileage record for vehicle ID {Vehicle.VehicleId}: StartMiles={Vehicle.MileageRecord.StartMiles}, EndingMiles=null");
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

            string? vinSuffix = await PageDialogService.DisplayPromptAsync(
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

            var authService = App.Services.GetRequiredService<AuthService>();
            bool assignmentSuccessful = false;

            while (!assignmentSuccessful)
            {
                (bool success, Vehicle? newVehicle, string message, List<MileageRecord>? incompleteRecords, int? mileageId) = await authService.AssignVehicleAsync(vinSuffix);

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
                        // Send refresh message
                        WeakReferenceMessenger.Default.Send(new RefreshVehiclePageMessage("force"));
                        await Shell.Current.GoToAsync($"Vehicle?refresh={Guid.NewGuid()}");
                    }
                    else
                    {
                        Console.WriteLine($"AssignVehicleByVin: Failed to submit start miles: {submitMessage}");
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
                            break;
                        }
                    }
                    else
                    {
                        Console.WriteLine($"AssignVehicleByVin: Failed to assign vehicle after resolving incomplete records: {message}");
                        string errorMessage = message;
                        if (message.Contains("Network error"))
                        {
                            errorMessage = "Network issue assigning vehicle. Please check your connection and try again.";
                        }
                        await PageDialogService.DisplayAlertAsync("Error", errorMessage, "OK");
                        break;
                    }
                }
                else
                {
                    Console.WriteLine($"AssignVehicleByVin: Failed to assign vehicle: {message}");
                    string errorMessage = message;
                    if (message.Contains("Network error"))
                    {
                        errorMessage = "Network issue assigning vehicle. Please check your connection and try again.";
                    }
                    await PageDialogService.DisplayAlertAsync("Error", errorMessage, "OK");
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
}