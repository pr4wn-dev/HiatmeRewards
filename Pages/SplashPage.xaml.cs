using HiatMeApp.Services;
using Microsoft.Maui.Storage;

namespace HiatMeApp;

public partial class SplashPage : ContentPage
{
    public SplashPage()
    {
        InitializeComponent();
    }

    protected override async void OnAppearing()
    {
        try
        {
            base.OnAppearing();
            
            // Small delay to show splash screen
            await Task.Delay(300);
        
        // Validate and restore session if logged in
        bool isLoggedIn = Preferences.Get("IsLoggedIn", false);
        var authToken = Preferences.Get("AuthToken", null);
        var userDataJson = Preferences.Get("UserData", string.Empty);
        Console.WriteLine($"SplashPage: OnAppearing - IsLoggedIn={isLoggedIn}, HasAuthToken={!string.IsNullOrEmpty(authToken)}, HasUserData={!string.IsNullOrEmpty(userDataJson)}");
        
        // If we have stored data but IsLoggedIn is false, try to restore anyway (might be a preference issue)
        if (!isLoggedIn && !string.IsNullOrEmpty(authToken) && !string.IsNullOrEmpty(userDataJson))
        {
            Console.WriteLine("SplashPage: Found stored auth token and user data but IsLoggedIn is false, attempting to validate");
            isLoggedIn = true; // Set to true so we attempt validation
        }
        
        if (isLoggedIn && !string.IsNullOrEmpty(authToken))
        {
            try
            {
                // Validate session with server
                var authService = App.Services.GetRequiredService<AuthService>();
                Console.WriteLine("SplashPage: Calling ValidateSessionAsync...");
                var (sessionValid, user, message) = await authService.ValidateSessionAsync();
                Console.WriteLine($"SplashPage: ValidateSessionAsync returned - Success={sessionValid}, HasUser={user != null}, Message={message}");
                
                if (sessionValid && user != null)
                {
                    App.CurrentUser = user;
                    Console.WriteLine($"SplashPage: Session validated and restored, Email={user.Email}, Role={user.Role}");
                    isLoggedIn = true;
                }
                else
                {
                    // Session validation failed - check if it's a genuine network error (can't reach server)
                    // vs. an invalid/expired session
                    // IMPORTANT: Only treat as network error if message explicitly says so AND doesn't mention expired/invalid
                    bool isExpiredOrInvalid = message.Contains("Session expired") || 
                                             message.Contains("Invalid token") ||
                                             message.Contains("expired") ||
                                             message.Contains("invalid") ||
                                             message.Contains("unauthorized") ||
                                             message.Contains("Unauthorized");
                    
                    bool isNetworkError = !isExpiredOrInvalid && 
                                         (message.Contains("Network error") || 
                                          message.Contains("Failed to retrieve session token") ||
                                          message.Contains("connection") ||
                                          message.Contains("timeout"));
                    
                    if (isExpiredOrInvalid)
                    {
                        // Session is definitely expired/invalid - clear login state
                        Console.WriteLine($"SplashPage: Session expired/invalid: {message}, clearing login state");
                        Preferences.Set("IsLoggedIn", false);
                        Preferences.Remove("AuthToken");
                        Preferences.Remove("UserData");
                        App.CurrentUser = null;
                        isLoggedIn = false;
                    }
                    else if (isNetworkError)
                    {
                        // Only restore from stored data if we genuinely can't reach the server
                        // This allows offline use, but we should still validate when online
                        Console.WriteLine($"SplashPage: Network error during validation (offline?), attempting to restore from stored data");
                        if (!string.IsNullOrEmpty(userDataJson))
                        {
                            try
                            {
                                var storedUser = Newtonsoft.Json.JsonConvert.DeserializeObject<Models.User>(userDataJson);
                                if (storedUser != null)
                                {
                                    App.CurrentUser = storedUser;
                                    Console.WriteLine($"SplashPage: Restored user from stored data (offline mode), Email={storedUser.Email}, Role={storedUser.Role}");
                                    isLoggedIn = true;
                                }
                            }
                            catch (Exception restoreEx)
                            {
                                Console.WriteLine($"SplashPage: Failed to restore from stored data: {restoreEx.Message}");
                                isLoggedIn = false;
                            }
                        }
                        else
                        {
                            isLoggedIn = false;
                        }
                    }
                    else
                    {
                        // Unknown error - assume session is invalid
                        Console.WriteLine($"SplashPage: Unknown validation error: {message}, clearing login state");
                        Preferences.Set("IsLoggedIn", false);
                        Preferences.Remove("AuthToken");
                        Preferences.Remove("UserData");
                        App.CurrentUser = null;
                        isLoggedIn = false;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"SplashPage: Exception validating session: {ex.Message}, StackTrace: {ex.StackTrace}");
                // Only restore from stored data if it's a network/connection exception, not a validation failure
                bool isNetworkException = ex is HttpRequestException || 
                                         ex is System.Net.Sockets.SocketException ||
                                         ex.Message.Contains("Network") ||
                                         ex.Message.Contains("connection") ||
                                         ex.Message.Contains("timeout");
                
                if (isNetworkException)
                {
                    // Network issue - allow offline mode with stored data
                    Console.WriteLine($"SplashPage: Network exception, attempting offline restore");
                    if (!string.IsNullOrEmpty(userDataJson))
                    {
                        try
                        {
                            var storedUser = Newtonsoft.Json.JsonConvert.DeserializeObject<Models.User>(userDataJson);
                            if (storedUser != null)
                            {
                                App.CurrentUser = storedUser;
                                Console.WriteLine($"SplashPage: Restored user from stored data (offline mode), Email={storedUser.Email}");
                                isLoggedIn = true;
                            }
                        }
                        catch
                        {
                            Console.WriteLine($"SplashPage: Failed to restore from stored data");
                        }
                    }
                }
                
                // If we couldn't restore or it wasn't a network error, clear login state
                if (!isLoggedIn)
                {
                    Console.WriteLine($"SplashPage: Clearing login state due to exception");
                    Preferences.Set("IsLoggedIn", false);
                    Preferences.Remove("AuthToken");
                    Preferences.Remove("UserData");
                    App.CurrentUser = null;
                    isLoggedIn = false;
                }
            }
        }
        else
        {
            Console.WriteLine($"SplashPage: Not logged in or no auth token - IsLoggedIn={isLoggedIn}, HasAuthToken={!string.IsNullOrEmpty(authToken)}");
        }
        
        // Update menu visibility if we have a shell
        if (Shell.Current is AppShell appShell && App.CurrentUser != null)
        {
            if (Shell.Current.BindingContext is ViewModels.AppShellViewModel vm)
            {
                vm.UpdateMenuItems();
            }
            appShell.UpdateMenuVisibility();
        }
        
            // Navigate to appropriate route based on login status
            string targetRoute = isLoggedIn ? "//Home" : "//Login";
            Console.WriteLine($"SplashPage: Navigating to {targetRoute}");
            try
            {
                await Shell.Current.GoToAsync(targetRoute);
                Console.WriteLine($"SplashPage: Navigation to {targetRoute} completed");
            }
            catch (Exception navEx)
            {
                Console.WriteLine($"SplashPage: Navigation error: {navEx.Message}, StackTrace: {navEx.StackTrace}");
                // Fallback to login if navigation fails
                try
                {
                    await Shell.Current.GoToAsync("//Login");
                }
                catch
                {
                    Console.WriteLine("SplashPage: Fallback navigation also failed");
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"SplashPage: Critical error in OnAppearing: {ex.Message}, StackTrace: {ex.StackTrace}");
            // Try to navigate to login as fallback
            try
            {
                await Shell.Current.GoToAsync("//Login");
            }
            catch
            {
                Console.WriteLine("SplashPage: Could not navigate to Login after error");
            }
        }
    }
}

