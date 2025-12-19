using Microsoft.Maui.Controls;
using Microsoft.Maui.Devices;
using HiatMeApp.ViewModels;

namespace HiatMeApp;

public partial class AppShell : Shell
{
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
        Console.WriteLine("AppShell: Initialized with routes");
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
    }

    private async void OnLoginClicked(object sender, EventArgs e)
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

    private async void OnHomeClicked(object sender, EventArgs e)
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
        if (FlyoutAvatarImage != null)
        {
            if (string.IsNullOrEmpty(profilePicture))
            {
                FlyoutAvatarImage.Source = ImageSource.FromFile("avatar.png");
            }
            else if (profilePicture.StartsWith("http://") || profilePicture.StartsWith("https://"))
            {
                FlyoutAvatarImage.Source = ImageSource.FromUri(new Uri(profilePicture));
            }
            else
            {
                FlyoutAvatarImage.Source = ImageSource.FromFile(profilePicture);
            }
        }
    }

    private void UpdateMenuVisibility()
    {
        bool isLoggedIn = Preferences.Get("IsLoggedIn", false);
        string? userRole = null;
        
        if (isLoggedIn)
        {
            var userJson = Preferences.Get("UserData", string.Empty);
            if (!string.IsNullOrEmpty(userJson))
            {
                try
                {
                    var user = Newtonsoft.Json.JsonConvert.DeserializeObject<Models.User>(userJson);
                    userRole = user?.Role;
                }
                catch { }
            }
        }

        // Add/Remove menu items based on login status and role
        // Note: MenuItem doesn't support IsVisible, so we manage the collection
        bool isManagerOrOwner = userRole == "Manager" || userRole == "Owner";
        
        // Home and Profile are always available when logged in
        // Manage Users, Vehicles, and Day Off Requests only for Managers/Owners
        // Login/Logout is always available
        
        // The menu items are defined in XAML, so we'll handle visibility through navigation logic
        // For now, all items remain in the menu but navigation will be restricted
    }

    private async void OnProfileClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("//Profile");
        Shell.Current.FlyoutIsPresented = false;
    }

    private void OnManageUsersClicked(object sender, EventArgs e)
    {
        Shell.Current.FlyoutIsPresented = false;
        // Check if user has permission (Manager or Owner)
        var userJson = Preferences.Get("UserData", string.Empty);
        if (!string.IsNullOrEmpty(userJson))
        {
            try
            {
                var user = Newtonsoft.Json.JsonConvert.DeserializeObject<Models.User>(userJson);
                if (user?.Role == "Manager" || user?.Role == "Owner")
                {
                    // Would open web view or external page for manage users
                    Console.WriteLine("ManageUsers: Would open manage users page");
                }
            }
            catch { }
        }
    }

    private void OnManageVehiclesClicked(object sender, EventArgs e)
    {
        Shell.Current.FlyoutIsPresented = false;
        // Check if user has permission (Manager or Owner)
        var userJson = Preferences.Get("UserData", string.Empty);
        if (!string.IsNullOrEmpty(userJson))
        {
            try
            {
                var user = Newtonsoft.Json.JsonConvert.DeserializeObject<Models.User>(userJson);
                if (user?.Role == "Manager" || user?.Role == "Owner")
                {
                    // Would open web view or external page for manage vehicles
                    Console.WriteLine("ManageVehicles: Would open manage vehicles page");
                }
            }
            catch { }
        }
    }

    private void OnDayOffRequestsClicked(object sender, EventArgs e)
    {
        Shell.Current.FlyoutIsPresented = false;
        // Check if user has permission (Manager or Owner)
        var userJson = Preferences.Get("UserData", string.Empty);
        if (!string.IsNullOrEmpty(userJson))
        {
            try
            {
                var user = Newtonsoft.Json.JsonConvert.DeserializeObject<Models.User>(userJson);
                if (user?.Role == "Manager" || user?.Role == "Owner")
                {
                    // Would open web view or external page for day off requests
                    Console.WriteLine("DayOffRequests: Would open day off requests page");
                }
            }
            catch { }
        }
    }
}