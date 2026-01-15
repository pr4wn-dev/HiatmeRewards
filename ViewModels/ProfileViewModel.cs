using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HiatMeApp.Helpers;
using HiatMeApp.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Media;
using Newtonsoft.Json;
using System;
using System.Threading.Tasks;

namespace HiatMeApp.ViewModels;

public partial class ProfileViewModel : BaseViewModel
{
    private readonly AuthService _authService;
    private string? _originalEmail;
    private FileResult? _selectedImageFile;

    [ObservableProperty]
    private string? _name;

    [ObservableProperty]
    private string? _email;

    [ObservableProperty]
    private string? _phone;

    [ObservableProperty]
    private string? _profilePicture;

    [ObservableProperty]
    private string? _role;

    [ObservableProperty]
    private bool _isEditing;

    public ProfileViewModel() : base()
    {
        Title = "Profile";
        _authService = App.Services.GetRequiredService<AuthService>();
        LoadUserData();
    }

    public void LoadUserData()
    {
        if (App.CurrentUser != null)
        {
            Name = App.CurrentUser.Name;
            Email = App.CurrentUser.Email;
            _originalEmail = App.CurrentUser.Email;
            Phone = App.CurrentUser.Phone;
            ProfilePicture = App.CurrentUser.ProfilePicture;
            Role = App.CurrentUser.Role;
            Console.WriteLine($"LoadUserData: App.CurrentUser exists");
            Console.WriteLine($"  - Name: {Name}");
            Console.WriteLine($"  - Email: {Email}");
            Console.WriteLine($"  - Phone: {Phone ?? "null"}");
            Console.WriteLine($"  - ProfilePicture: '{ProfilePicture ?? "null"}' (length: {ProfilePicture?.Length ?? 0})");
            Console.WriteLine($"  - Role: {Role}");
        }
        else
        {
            Console.WriteLine("LoadUserData: App.CurrentUser is null, loading from preferences");
            // Try to load from preferences
            var userJson = Preferences.Get("UserData", string.Empty);
            if (!string.IsNullOrEmpty(userJson))
            {
                Console.WriteLine($"LoadUserData: UserData JSON length: {userJson.Length}");
                var user = JsonConvert.DeserializeObject<Models.User>(userJson);
                if (user != null)
                {
                    Name = user.Name;
                    Email = user.Email;
                    _originalEmail = user.Email;
                    Phone = user.Phone;
                    ProfilePicture = user.ProfilePicture;
                    Role = user.Role;
                    Console.WriteLine($"LoadUserData: Loaded from preferences");
                    Console.WriteLine($"  - Name: {Name}");
                    Console.WriteLine($"  - Email: {Email}");
                    Console.WriteLine($"  - Phone: {Phone ?? "null"}");
                    Console.WriteLine($"  - ProfilePicture: '{ProfilePicture ?? "null"}' (length: {ProfilePicture?.Length ?? 0})");
                    Console.WriteLine($"  - Role: {Role}");
                }
                else
                {
                    Console.WriteLine("LoadUserData: Failed to deserialize user from preferences");
                }
            }
            else
            {
                Console.WriteLine("LoadUserData: No user data in preferences");
            }
        }
        
        // Force property change notification for ProfilePicture
        OnPropertyChanged(nameof(ProfilePicture));
    }

    [RelayCommand]
    private void StartEditing()
    {
        IsEditing = true;
        Console.WriteLine("StartEditing: Entered edit mode");
    }

    [RelayCommand]
    private void CancelEditing()
    {
        // Reload original data
        LoadUserData();
        _selectedImageFile = null;
        IsEditing = false;
        Console.WriteLine("CancelEditing: Cancelled edit, reloaded original data");
    }

    [RelayCommand]
    private async Task PickImage()
    {
        try
        {
            var result = await MediaPicker.PickPhotoAsync(new MediaPickerOptions
            {
                Title = "Select Profile Picture"
            });

            if (result != null)
            {
                _selectedImageFile = result;
                // Display the selected image immediately using the local file path
                if (!string.IsNullOrEmpty(result.FullPath))
                {
                    ProfilePicture = result.FullPath;
                }
                Console.WriteLine($"PickImage: Selected image: {result.FileName}, Path: {result.FullPath}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"PickImage: Error picking image: {ex.Message}");
            await PageDialogService.DisplayAlertAsync("Error", "Failed to pick image.", "OK");
        }
    }

    [RelayCommand]
    private async Task SaveProfile()
    {
        try
        {
            if (IsBusy) return;

            if (string.IsNullOrWhiteSpace(Name) || string.IsNullOrWhiteSpace(Email))
            {
                await PageDialogService.DisplayAlertAsync("Error", "Name and Email are required.", "OK");
                return;
            }

            if (string.IsNullOrWhiteSpace(_originalEmail))
            {
                await PageDialogService.DisplayAlertAsync("Error", "Current email not found. Please log in again.", "OK");
                return;
            }

            IsBusy = true;
            Console.WriteLine($"SaveProfile: Saving profile for currentEmail={_originalEmail}, email={Email}, name={Name}, hasImage={_selectedImageFile != null}");

            var (success, updatedUser, message) = await _authService.UpdateProfileAsync(
                _originalEmail,
                Name,
                Email,
                Phone,
                _selectedImageFile
            );

            if (success && updatedUser != null)
            {
                Console.WriteLine($"SaveProfile: Profile updated successfully");
                // Update all properties from the server response
                Name = updatedUser.Name;
                Email = updatedUser.Email;
                Phone = updatedUser.Phone;
                ProfilePicture = updatedUser.ProfilePicture; // Use server URL instead of local path
                _originalEmail = updatedUser.Email; // Update original email in case it changed
                _selectedImageFile = null; // Clear selected file
                IsEditing = false;
                
                // Update the flyout menu header with new user info
                if (Shell.Current?.BindingContext is AppShellViewModel shellVm)
                {
                    shellVm.UpdateMenuItems();
                    Console.WriteLine($"SaveProfile: Updated AppShellViewModel menu items");
                }
                
                await PageDialogService.DisplayAlertAsync("Success", message, "OK");
            }
            else
            {
                Console.WriteLine($"SaveProfile: Failed to update profile: {message}");
                // Don't show another popup if it's a "logged in elsewhere" error - that popup was already shown
                if (!message.StartsWith("LOGGED_IN_ELSEWHERE:", StringComparison.OrdinalIgnoreCase))
                {
                    await PageDialogService.DisplayAlertAsync("Error", message, "OK");
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"SaveProfile: Error: {ex.Message}, StackTrace: {ex.StackTrace}");
            await PageDialogService.DisplayAlertAsync("Error", "Failed to save profile.", "OK");
        }
        finally
        {
            IsBusy = false;
        }
    }
}

