using Microsoft.Maui.Controls;
using Microsoft.Maui.Devices;
using Microsoft.Maui.Graphics;
using HiatMeApp.ViewModels;
using System.Collections.Generic;
using System.Reflection;

namespace HiatMeApp;

public partial class AppShell : Shell
{
    private MenuItem? _homeMenuItem;
    private MenuItem? _profileMenuItem;
    private MenuItem? _requestDayOffMenuItem;
    private MenuItem? _loginLogoutMenuItem; // Single item that changes text
    private MenuItem? _registerMenuItem;
    private readonly object _menuUpdateLock = new object();
    private bool _isUpdatingMenu = false;

    public AppShell()
    {
        InitializeComponent();
        Routing.RegisterRoute("Login", typeof(LoginPage));
        Routing.RegisterRoute("Register", typeof(RegisterPage));
        Routing.RegisterRoute("ForgotPassword", typeof(ForgotPasswordPage));
        Routing.RegisterRoute("Home", typeof(HomePage));
        Routing.RegisterRoute("Vehicle", typeof(VehiclePage)); // Updated
        Routing.RegisterRoute("FinishDay", typeof(FinishDayPage));
        Routing.RegisterRoute("VehicleIssues", typeof(VehicleIssuesPage));
        Routing.RegisterRoute("Profile", typeof(ProfilePage));
        Routing.RegisterRoute("RequestDayOff", typeof(RequestDayOffPage));
        Console.WriteLine("AppShell: Initialized with routes");
        
        // Create menu items programmatically (will be added/removed based on login status)
        _homeMenuItem = new MenuItem { Text = "Home" };
        _homeMenuItem.Clicked += OnHomeClicked;
        
        _profileMenuItem = new MenuItem { Text = "Profile" };
        _profileMenuItem.Clicked += OnProfileClicked;
        
        _requestDayOffMenuItem = new MenuItem { Text = "Request Day Off" };
        _requestDayOffMenuItem.Clicked += OnRequestDayOffClicked;
        
        // Single Login/Logout menu item that changes text
        _loginLogoutMenuItem = new MenuItem { Text = "Login" };
        _loginLogoutMenuItem.Clicked += OnLoginClicked;
        
        _registerMenuItem = new MenuItem { Text = "Register" };
        _registerMenuItem.Clicked += OnRegisterClicked;
        
        Loaded += async (s, e) =>
        {
            Console.WriteLine($"AppShell: Loaded, BindingContext={BindingContext?.GetType()?.Name ?? "null"}");
            if (BindingContext is AppShellViewModel vm)
            {
                vm.UpdateMenuItems();
                vm.PropertyChanged += OnViewModelPropertyChanged;
                UpdateFlyoutAvatar(vm.ProfilePicture);
                UpdateMenuVisibility();
                Console.WriteLine($"AppShell: Updated menu items, LoginMenuTitle={vm.LoginMenuTitle}");
            }
            
            // Set flyout width after loaded to ensure it fills screen on mobile
            // Use MainThread to ensure UI is ready
            MainThread.BeginInvokeOnMainThread(() =>
            {
                if (DeviceInfo.Idiom == DeviceIdiom.Phone)
                {
                    // Calculate screen width in device-independent units
                    var displayInfo = DeviceDisplay.MainDisplayInfo;
                    var screenWidth = displayInfo.Width / displayInfo.Density;
                    this.FlyoutWidth = screenWidth;
                    Console.WriteLine($"AppShell: Set FlyoutWidth to {screenWidth} (screen width, density={displayInfo.Density})");
                }
            });
            
            try
            {
                await Shell.Current.GoToAsync("//Login");
                Console.WriteLine("AppShell: Initial navigation to Login succeeded");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"AppShell: Initial navigation error: {ex.Message}");
            }
        };
        
        BindingContextChanged += (s, e) =>
        {
            if (BindingContext is AppShellViewModel vm)
            {
                vm.PropertyChanged += OnViewModelPropertyChanged;
                UpdateFlyoutAvatar(vm.ProfilePicture);
                UpdateMenuVisibility();
            }
        };
        
        // Set flyout width to fill screen on mobile (use -1 for full width)
        if (DeviceInfo.Idiom == DeviceIdiom.Phone)
        {
            this.FlyoutWidth = -1; // Full width on mobile
        }
        else
        {
            this.FlyoutWidth = 300; // Fixed width on desktop
        }
        
        // Ensure flyout background is WebsiteDark (dark gray #333333) to match website
        // Note: This is also set in Styles.xaml, but explicitly setting here to ensure it's applied
        this.FlyoutBackgroundColor = Color.FromArgb("#333333");
    }

    private async void OnLoginClicked(object? sender, EventArgs e)
    {
        Console.WriteLine("AppShell: OnLoginClicked triggered");
        try
        {
            if (Preferences.Get("IsLoggedIn", false))
            {
                // Logout
                Preferences.Clear();
                Console.WriteLine("AppShell: Preferences cleared on logout");
                if (BindingContext is AppShellViewModel vm)
                {
                    vm.UpdateMenuItems();
                }
                UpdateMenuVisibility(); // This will update the text to "Login"
            }
            else
            {
                // Login - just navigate
            }
            Shell.Current.FlyoutIsPresented = false;
            await Shell.Current.GoToAsync("//Login");
            Console.WriteLine("AppShell: Navigated to Login");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"AppShell: OnLoginClicked error: {ex.Message}");
        }
    }

    private async void OnHomeClicked(object? sender, EventArgs e)
    {
        Console.WriteLine("AppShell: OnHomeClicked triggered");
        try
        {
            var route = Preferences.Get("IsLoggedIn", false) ? "//Home" : "//Login";
            Shell.Current.FlyoutIsPresented = false;
            await Shell.Current.GoToAsync(route);
            Console.WriteLine($"AppShell: Navigated to {route}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"AppShell: OnHomeClicked error: {ex.Message}");
        }
    }

    private async void OnTestLoginClicked(object sender, EventArgs e)
    {
        Console.WriteLine("AppShell: OnTestLoginClicked triggered");
        try
        {
            if (Preferences.Get("IsLoggedIn", false))
            {
                Preferences.Clear();
                Console.WriteLine("AppShell: Preferences cleared on test logout");
                if (BindingContext is AppShellViewModel vm)
                {
                    vm.UpdateMenuItems();
                }
            }
            await Shell.Current.GoToAsync("//Login");
            Console.WriteLine("AppShell: Test navigated to Login");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"AppShell: OnTestLoginClicked error: {ex.Message}");
        }
    }

    private async void OnTestHomeClicked(object sender, EventArgs e)
    {
        Console.WriteLine("AppShell: OnTestHomeClicked triggered");
        try
        {
            var route = Preferences.Get("IsLoggedIn", false) ? "//Home" : "//Login";
            await Shell.Current.GoToAsync(route);
            Console.WriteLine($"AppShell: Test navigated to {route}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"AppShell: OnTestHomeClicked error: {ex.Message}");
        }
    }

    private void OnViewModelPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(AppShellViewModel.ProfilePicture) && sender is AppShellViewModel vm)
        {
            UpdateFlyoutAvatar(vm.ProfilePicture);
        }
    }

    private void UpdateFlyoutAvatar(string? profilePicture)
    {
        // Find the image in the flyout header
        var flyoutHeader = this.FlyoutHeader as Grid;
        if (flyoutHeader != null)
        {
            var image = flyoutHeader.FindByName("FlyoutAvatarImage") as Image;
            if (image != null)
            {
                if (string.IsNullOrEmpty(profilePicture))
                {
                    // avatar.png is excluded from build due to corruption - use a placeholder or leave empty
                    // You can replace this with a valid PNG file in Resources/Images/avatar.png
                    try
                    {
                        image.Source = ImageSource.FromFile("avatar.png");
                    }
                    catch
                    {
                        // If avatar.png doesn't exist or is invalid, leave image source as default
                        // or set to null to use a placeholder
                        image.Source = null;
                    }
                }
                else if (profilePicture.StartsWith("http://") || profilePicture.StartsWith("https://"))
                {
                    image.Source = ImageSource.FromUri(new Uri(profilePicture));
                }
                else
                {
                    image.Source = ImageSource.FromFile(profilePicture);
                }
            }
        }
    }

    public void UpdateMenuVisibility()
    {
        // Prevent concurrent execution
        lock (_menuUpdateLock)
        {
            if (_isUpdatingMenu)
            {
                Console.WriteLine("UpdateMenuVisibility: Already updating, skipping");
                return;
            }
            _isUpdatingMenu = true;
        }
        
        try
        {
            // Ensure this runs on the main thread
            if (!MainThread.IsMainThread)
            {
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    lock (_menuUpdateLock)
                    {
                        _isUpdatingMenu = false;
                    }
                    UpdateMenuVisibility();
                });
                return;
            }
            
            bool isLoggedIn = Preferences.Get("IsLoggedIn", false);
            
            // Remove ALL our menu items by trying multiple strategies
            try
            {
                Console.WriteLine($"UpdateMenuVisibility: Starting removal. Items.Count={Items.Count}");
                
                // Strategy 1: Try direct reference comparison (most reliable if it works)
                var itemsToRemoveByRef = new List<ShellItem>();
                for (int i = 0; i < Items.Count; i++)
                {
                    var item = Items[i];
                    // Try comparing as object references
                    if (object.ReferenceEquals(item, _homeMenuItem) ||
                        object.ReferenceEquals(item, _profileMenuItem) ||
                        object.ReferenceEquals(item, _requestDayOffMenuItem) ||
                        object.ReferenceEquals(item, _loginLogoutMenuItem) ||
                        object.ReferenceEquals(item, _registerMenuItem))
                    {
                        itemsToRemoveByRef.Add(item);
                        Console.WriteLine($"UpdateMenuVisibility: Found menu item by reference at index {i}");
                    }
                }
                
                // Remove items found by reference
                foreach (var item in itemsToRemoveByRef)
                {
                    Items.Remove(item);
                }
                
                // Strategy 2: Remove by text matching (fallback for cases where reference doesn't work)
                var ourMenuTexts = new HashSet<string> { "Home", "Profile", "Request Day Off", "Login", "Logout", "Register" };
                for (int i = Items.Count - 1; i >= 0; i--)
                {
                    var item = Items[i];
                    var itemType = item.GetType();
                    
                    // Check if this looks like a MenuItem
                    if (itemType.Name == "MenuItem")
                    {
                        var textProperty = itemType.GetProperty("Text");
                        if (textProperty != null)
                        {
                            var text = textProperty.GetValue(item) as string;
                            if (!string.IsNullOrEmpty(text) && ourMenuTexts.Contains(text))
                            {
                                Console.WriteLine($"UpdateMenuVisibility: Removing menu item '{text}' by text match");
                                Items.RemoveAt(i);
                            }
                        }
                    }
                }
                
                Console.WriteLine($"UpdateMenuVisibility: After removal. Items.Count={Items.Count}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"UpdateMenuVisibility: Error removing items: {ex.Message}, StackTrace: {ex.StackTrace}");
            }
            
            // Update Login/Logout text
            if (_loginLogoutMenuItem != null)
            {
                _loginLogoutMenuItem.Text = isLoggedIn ? "Logout" : "Login";
            }
            
            // Now add items in the correct order based on login status
            try
            {
                if (isLoggedIn)
                {
                    // When logged in: Home, Profile, Request Day Off, Logout
                    if (_homeMenuItem != null)
                    {
                        Items.Add(_homeMenuItem);
                        Console.WriteLine("UpdateMenuVisibility: Added Home");
                    }
                    if (_profileMenuItem != null)
                    {
                        Items.Add(_profileMenuItem);
                        Console.WriteLine("UpdateMenuVisibility: Added Profile");
                    }
                    if (_requestDayOffMenuItem != null)
                    {
                        Items.Add(_requestDayOffMenuItem);
                        Console.WriteLine("UpdateMenuVisibility: Added Request Day Off");
                    }
                    if (_loginLogoutMenuItem != null)
                    {
                        Items.Add(_loginLogoutMenuItem);
                        Console.WriteLine("UpdateMenuVisibility: Added Logout");
                    }
                }
                else
                {
                    // When logged out: Login and Register only
                    if (_loginLogoutMenuItem != null)
                    {
                        Items.Add(_loginLogoutMenuItem);
                        Console.WriteLine("UpdateMenuVisibility: Added Login");
                    }
                    if (_registerMenuItem != null)
                    {
                        Items.Add(_registerMenuItem);
                        Console.WriteLine("UpdateMenuVisibility: Added Register");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"UpdateMenuVisibility: Error adding items: {ex.Message}");
            }
            
            Console.WriteLine($"UpdateMenuVisibility: IsLoggedIn={isLoggedIn}, TotalItemsCount={Items.Count}");
        }
        finally
        {
            lock (_menuUpdateLock)
            {
                _isUpdatingMenu = false;
            }
        }
    }
    
    private async void OnRegisterClicked(object? sender, EventArgs e)
    {
        try
        {
            Shell.Current.FlyoutIsPresented = false;
            await Shell.Current.GoToAsync("//Register");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"OnRegisterClicked: Error navigating: {ex.Message}");
        }
    }

    private async void OnProfileClicked(object? sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("//Profile");
        Shell.Current.FlyoutIsPresented = false;
    }

    private async void OnRequestDayOffClicked(object? sender, EventArgs e)
    {
        try
        {
            Shell.Current.FlyoutIsPresented = false;
            await Shell.Current.GoToAsync("//RequestDayOff");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"OnRequestDayOffClicked: Error navigating: {ex.Message}");
        }
    }
}