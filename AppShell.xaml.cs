using Microsoft.Maui.Controls;
using Microsoft.Maui.Devices;
using Microsoft.Maui.Graphics;
using HiatMeApp.ViewModels;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using System.Threading.Tasks;
using System.Net.Http;
using System.IO;
#if ANDROID
using HiatMeApp.Platforms.Android;
#endif

namespace HiatMeApp;

public partial class AppShell : Shell
{
    private MenuItem? _homeMenuItem;
    private MenuItem? _profileMenuItem;
    private MenuItem? _requestDayOffMenuItem;
    private MenuItem? _vehicleMenuItem;
    private MenuItem? _vehicleIssuesMenuItem;
    private MenuItem? _finishDayMenuItem;
    private MenuItem? _loginLogoutMenuItem; // Single item that changes text
    private MenuItem? _registerMenuItem;
    private readonly object _menuUpdateLock = new object();
    private bool _isUpdatingMenu = false;
    private readonly HashSet<ShellItem> _addedMenuItems = new HashSet<ShellItem>(); // Track what we've added

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
        
        _vehicleMenuItem = new MenuItem { Text = "Vehicle" };
        _vehicleMenuItem.Clicked += OnVehicleClicked;
        
        _vehicleIssuesMenuItem = new MenuItem { Text = "Vehicle Issues" };
        _vehicleIssuesMenuItem.Clicked += OnVehicleIssuesClicked;
        
        _finishDayMenuItem = new MenuItem { Text = "Finish Day" };
        _finishDayMenuItem.Clicked += OnFinishDayClicked;
        
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
            
            // Style menu items on app start (Android only)
#if ANDROID
            MainThread.BeginInvokeOnMainThread(() =>
            {
                // Delay to ensure menu is rendered
                Task.Delay(500).ContinueWith(_ =>
                {
                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        MenuStyler.StyleMenuItems(this);
                    });
                });
            });
#endif
            
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
        
        // Style menu items when flyout is opened and when navigation changes (Android only)
#if ANDROID
        // Style when flyout opens - use async task with longer delay to ensure menu is fully rendered
        this.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(FlyoutIsPresented) && this.FlyoutIsPresented)
            {
                // Delay styling until menu is fully open and rendered
                _ = Task.Run(async () =>
                {
                    await Task.Delay(500); // Longer delay to ensure menu is fully rendered
                    if (this.FlyoutIsPresented) // Double-check menu is still open
                    {
                        MainThread.BeginInvokeOnMainThread(() =>
                        {
                            try
                            {
                                MenuStyler.StyleMenuItems(this);
                            }
                            catch (Exception ex)
                            {
                                System.Diagnostics.Debug.WriteLine($"Error styling menu: {ex.Message}");
                            }
                        });
                    }
                });
            }
        };
        
        // Update menu styling when navigation changes (only if flyout is open)
        this.Navigated += (s, e) =>
        {
            if (this.FlyoutIsPresented)
            {
                _ = Task.Run(async () =>
                {
                    await Task.Delay(300);
                    if (this.FlyoutIsPresented) // Double-check menu is still open
                    {
                        MainThread.BeginInvokeOnMainThread(() =>
                        {
                            try
                            {
                                MenuStyler.StyleMenuItems(this);
                            }
                            catch (Exception ex)
                            {
                                System.Diagnostics.Debug.WriteLine($"Error styling menu on navigation: {ex.Message}");
                            }
                        });
                    }
                });
            }
        };
#endif
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

    private async void UpdateFlyoutAvatar(string? profilePicture)
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
                    // Load default avatar from website using HttpClient to avoid PNG validation issues
                    try
                    {
                        using (var httpClient = new HttpClient())
                        {
                            var imageBytes = await httpClient.GetByteArrayAsync("https://hiatme.com/images/avatar.png");
                            image.Source = ImageSource.FromStream(() => new MemoryStream(imageBytes));
                        }
                    }
                    catch
                    {
                        // Fallback to icon if download fails
                        image.Source = new FontImageSource
                        {
                            FontFamily = "MaterialIcons",
                            Glyph = "\ue853",
                            Size = 50,
                            Color = Color.FromArgb("#0078D4")
                        };
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
            
            // Remove ALL tracked menu items first
            Console.WriteLine($"UpdateMenuVisibility: Removing {_addedMenuItems.Count} tracked menu items");
            foreach (var trackedItem in _addedMenuItems.ToList())
            {
                try
                {
                    if (Items.Contains(trackedItem))
                    {
                        Items.Remove(trackedItem);
                        Console.WriteLine($"UpdateMenuVisibility: Removed tracked item");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"UpdateMenuVisibility: Error removing tracked item: {ex.Message}");
                }
            }
            _addedMenuItems.Clear();
            
            // Also remove by text matching as backup (brute force) - but be more careful
            // Only remove if we haven't already removed it via tracking
            var ourMenuTexts = new HashSet<string> { "Home", "Profile", "Request Day Off", "Vehicle", "Vehicle Issues", "Finish Day", "Login", "Logout", "Register" };
            int removedByText = 0;
            for (int i = Items.Count - 1; i >= 0; i--)
            {
                try
                {
                    var item = Items[i];
                    var itemType = item.GetType();
                    
                    // Skip if this is a ShellContent (not a MenuItem)
                    if (itemType.Name == "ShellContent")
                    {
                        continue;
                    }
                    
                    // Try to get Text property
                    var textProperty = itemType.GetProperty("Text", BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy);
                    if (textProperty == null && itemType.BaseType != null)
                    {
                        textProperty = itemType.BaseType.GetProperty("Text", BindingFlags.Public | BindingFlags.Instance);
                    }
                    
                    if (textProperty != null)
                    {
                        var text = textProperty.GetValue(item) as string;
                        if (!string.IsNullOrEmpty(text) && ourMenuTexts.Contains(text))
                        {
                            // Only remove if we haven't already tracked and removed this item
                            if (!_addedMenuItems.Contains(item))
                            {
                                Console.WriteLine($"UpdateMenuVisibility: REMOVING by text (backup): '{text}'");
                                Items.RemoveAt(i);
                                removedByText++;
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"UpdateMenuVisibility: Error checking item: {ex.Message}");
                }
            }
            
            if (removedByText > 0)
            {
                Console.WriteLine($"UpdateMenuVisibility: Removed {removedByText} items by text matching (backup method)");
            }
            
            Console.WriteLine($"UpdateMenuVisibility: After removal. Items.Count={Items.Count}");
            
            // Update Login/Logout text
            if (_loginLogoutMenuItem != null)
            {
                _loginLogoutMenuItem.Text = isLoggedIn ? "Logout" : "Login";
            }
            
            // Now add items in the correct order based on login status
            try
            {
                Console.WriteLine($"UpdateMenuVisibility: About to add items. isLoggedIn={isLoggedIn}");
                Console.WriteLine($"UpdateMenuVisibility: Menu item null checks - Home={_homeMenuItem == null}, Profile={_profileMenuItem == null}, RequestDayOff={_requestDayOffMenuItem == null}, Vehicle={_vehicleMenuItem == null}, VehicleIssues={_vehicleIssuesMenuItem == null}, FinishDay={_finishDayMenuItem == null}, LoginLogout={_loginLogoutMenuItem == null}, Register={_registerMenuItem == null}");
                
                if (isLoggedIn)
                {
                    // When logged in: Home, Profile, Request Day Off, Vehicle, Vehicle Issues, Finish Day, Logout
                    if (_homeMenuItem != null)
                    {
                        try
                        {
                            Items.Add(_homeMenuItem);
                            _addedMenuItems.Add(_homeMenuItem);
                            Console.WriteLine("UpdateMenuVisibility: ✓ Added Home");
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"UpdateMenuVisibility: ✗ Failed to add Home: {ex.Message}");
                        }
                    }
                    else
                    {
                        Console.WriteLine("UpdateMenuVisibility: ✗ Home menu item is NULL");
                    }
                    
                    if (_profileMenuItem != null)
                    {
                        try
                        {
                            Items.Add(_profileMenuItem);
                            _addedMenuItems.Add(_profileMenuItem);
                            Console.WriteLine("UpdateMenuVisibility: ✓ Added Profile");
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"UpdateMenuVisibility: ✗ Failed to add Profile: {ex.Message}");
                        }
                    }
                    else
                    {
                        Console.WriteLine("UpdateMenuVisibility: ✗ Profile menu item is NULL");
                    }
                    
                    if (_requestDayOffMenuItem != null)
                    {
                        try
                        {
                            Items.Add(_requestDayOffMenuItem);
                            _addedMenuItems.Add(_requestDayOffMenuItem);
                            Console.WriteLine("UpdateMenuVisibility: ✓ Added Request Day Off");
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"UpdateMenuVisibility: ✗ Failed to add Request Day Off: {ex.Message}");
                        }
                    }
                    else
                    {
                        Console.WriteLine("UpdateMenuVisibility: ✗ Request Day Off menu item is NULL");
                    }
                    
                    if (_vehicleMenuItem != null)
                    {
                        try
                        {
                            Items.Add(_vehicleMenuItem);
                            _addedMenuItems.Add(_vehicleMenuItem);
                            Console.WriteLine("UpdateMenuVisibility: ✓ Added Vehicle");
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"UpdateMenuVisibility: ✗ Failed to add Vehicle: {ex.Message}");
                        }
                    }
                    else
                    {
                        Console.WriteLine("UpdateMenuVisibility: ✗ Vehicle menu item is NULL");
                    }
                    
                    if (_vehicleIssuesMenuItem != null)
                    {
                        try
                        {
                            Items.Add(_vehicleIssuesMenuItem);
                            _addedMenuItems.Add(_vehicleIssuesMenuItem);
                            Console.WriteLine("UpdateMenuVisibility: ✓ Added Vehicle Issues");
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"UpdateMenuVisibility: ✗ Failed to add Vehicle Issues: {ex.Message}");
                        }
                    }
                    else
                    {
                        Console.WriteLine("UpdateMenuVisibility: ✗ Vehicle Issues menu item is NULL");
                    }
                    
                    if (_finishDayMenuItem != null)
                    {
                        try
                        {
                            Items.Add(_finishDayMenuItem);
                            _addedMenuItems.Add(_finishDayMenuItem);
                            Console.WriteLine("UpdateMenuVisibility: ✓ Added Finish Day");
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"UpdateMenuVisibility: ✗ Failed to add Finish Day: {ex.Message}");
                        }
                    }
                    else
                    {
                        Console.WriteLine("UpdateMenuVisibility: ✗ Finish Day menu item is NULL");
                    }
                    
                    if (_loginLogoutMenuItem != null)
                    {
                        try
                        {
                            Items.Add(_loginLogoutMenuItem);
                            _addedMenuItems.Add(_loginLogoutMenuItem);
                            Console.WriteLine("UpdateMenuVisibility: ✓ Added Logout");
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"UpdateMenuVisibility: ✗ Failed to add Logout: {ex.Message}");
                        }
                    }
                    else
                    {
                        Console.WriteLine("UpdateMenuVisibility: ✗ Login/Logout menu item is NULL");
                    }
                }
                else
                {
                    // When logged out: Login and Register only
                    if (_loginLogoutMenuItem != null)
                    {
                        try
                        {
                            Items.Add(_loginLogoutMenuItem);
                            _addedMenuItems.Add(_loginLogoutMenuItem);
                            Console.WriteLine("UpdateMenuVisibility: ✓ Added Login");
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"UpdateMenuVisibility: ✗ Failed to add Login: {ex.Message}");
                        }
                    }
                    else
                    {
                        Console.WriteLine("UpdateMenuVisibility: ✗ Login menu item is NULL");
                    }
                    
                    if (_registerMenuItem != null)
                    {
                        try
                        {
                            Items.Add(_registerMenuItem);
                            _addedMenuItems.Add(_registerMenuItem);
                            Console.WriteLine("UpdateMenuVisibility: ✓ Added Register");
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"UpdateMenuVisibility: ✗ Failed to add Register: {ex.Message}");
                        }
                    }
                    else
                    {
                        Console.WriteLine("UpdateMenuVisibility: ✗ Register menu item is NULL");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"UpdateMenuVisibility: Error adding items: {ex.Message}, StackTrace: {ex.StackTrace}");
            }
            
            Console.WriteLine($"UpdateMenuVisibility: IsLoggedIn={isLoggedIn}, TotalItemsCount={Items.Count}");
            
            // Style menu items on Android
#if ANDROID
            MenuStyler.StyleMenuItems(this);
#endif
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

    private async void OnVehicleClicked(object? sender, EventArgs e)
    {
        try
        {
            Shell.Current.FlyoutIsPresented = false;
            await Shell.Current.GoToAsync("//Vehicle");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"OnVehicleClicked: Error navigating: {ex.Message}");
        }
    }

    private async void OnVehicleIssuesClicked(object? sender, EventArgs e)
    {
        try
        {
            Shell.Current.FlyoutIsPresented = false;
            await Shell.Current.GoToAsync("//VehicleIssues");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"OnVehicleIssuesClicked: Error navigating: {ex.Message}");
        }
    }

    private async void OnFinishDayClicked(object? sender, EventArgs e)
    {
        try
        {
            Shell.Current.FlyoutIsPresented = false;
            await Shell.Current.GoToAsync("//FinishDay");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"OnFinishDayClicked: Error navigating: {ex.Message}");
        }
    }
}