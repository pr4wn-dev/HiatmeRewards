using Microsoft.Maui.Controls;
using Microsoft.Maui.Devices;
using Microsoft.Maui.Graphics;
using HiatMeApp.ViewModels;

namespace HiatMeApp;

public partial class AppShell : Shell
{
    private MenuItem? _homeMenuItem;
    private MenuItem? _profileMenuItem;
    private MenuItem? _requestDayOffMenuItem;
    private MenuItem? _logoutMenuItem;
    private MenuItem? _loginMenuItem;

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
        
        _loginMenuItem = new MenuItem { Text = "Login" };
        _loginMenuItem.Clicked += OnLoginClicked;
        
        _logoutMenuItem = new MenuItem { Text = "Logout" };
        _logoutMenuItem.Clicked += OnLoginClicked; // Reuse the same handler
        
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
                Preferences.Clear();
                Console.WriteLine("AppShell: Preferences cleared on logout");
                if (BindingContext is AppShellViewModel vm)
                {
                    vm.UpdateMenuItems();
                }
                UpdateMenuVisibility();
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
        // Ensure this runs on the main thread
        if (!MainThread.IsMainThread)
        {
            MainThread.BeginInvokeOnMainThread(UpdateMenuVisibility);
            return;
        }
        
        bool isLoggedIn = Preferences.Get("IsLoggedIn", false);
        
        // Show/hide menu items based on login status
        // MenuItem doesn't have IsVisible, so we need to add/remove from the Items collection
        // Order: Home (top), Profile, Request Day Off, then Logout (bottom) when logged in
        // Only Login when logged out
        
        // First, remove all our dynamic menu items to avoid duplicates
        // Use try-catch in case items aren't in the collection
        try
        {
            if (_homeMenuItem != null && Items.Contains(_homeMenuItem))
                Items.Remove(_homeMenuItem);
            if (_profileMenuItem != null && Items.Contains(_profileMenuItem))
                Items.Remove(_profileMenuItem);
            if (_requestDayOffMenuItem != null && Items.Contains(_requestDayOffMenuItem))
                Items.Remove(_requestDayOffMenuItem);
            if (_loginMenuItem != null && Items.Contains(_loginMenuItem))
                Items.Remove(_loginMenuItem);
            if (_logoutMenuItem != null && Items.Contains(_logoutMenuItem))
                Items.Remove(_logoutMenuItem);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"UpdateMenuVisibility: Error removing items: {ex.Message}");
        }
        
        // Now add items in the correct order based on login status
        try
        {
            if (isLoggedIn)
            {
                // When logged in: Home, Profile, Request Day Off, Logout
                if (_homeMenuItem != null && !Items.Contains(_homeMenuItem))
                    Items.Insert(0, _homeMenuItem);
                if (_profileMenuItem != null && !Items.Contains(_profileMenuItem))
                {
                    int insertIndex = Items.IndexOf(_homeMenuItem) >= 0 ? Items.IndexOf(_homeMenuItem) + 1 : Items.Count;
                    Items.Insert(insertIndex, _profileMenuItem);
                }
                if (_requestDayOffMenuItem != null && !Items.Contains(_requestDayOffMenuItem))
                {
                    int insertIndex = Items.IndexOf(_profileMenuItem) >= 0 ? Items.IndexOf(_profileMenuItem) + 1 : Items.Count;
                    Items.Insert(insertIndex, _requestDayOffMenuItem);
                }
                if (_logoutMenuItem != null && !Items.Contains(_logoutMenuItem))
                    Items.Add(_logoutMenuItem);
            }
            else
            {
                // When logged out: only Login
                if (_loginMenuItem != null && !Items.Contains(_loginMenuItem))
                    Items.Add(_loginMenuItem);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"UpdateMenuVisibility: Error adding items: {ex.Message}");
        }
        
        Console.WriteLine($"UpdateMenuVisibility: IsLoggedIn={isLoggedIn}, MenuItemsCount={Items.Count}");
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