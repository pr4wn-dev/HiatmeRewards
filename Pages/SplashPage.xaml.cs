using HiatMeApp.Services;
using Microsoft.Maui.Storage;
using System.IO;

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
        LogMessage($"SplashPage: OnAppearing - IsLoggedIn={isLoggedIn}, HasAuthToken={!string.IsNullOrEmpty(authToken)}, HasUserData={!string.IsNullOrEmpty(userDataJson)}");
        Console.WriteLine($"SplashPage: OnAppearing - IsLoggedIn={isLoggedIn}, HasAuthToken={!string.IsNullOrEmpty(authToken)}, HasUserData={!string.IsNullOrEmpty(userDataJson)}");
        
        // If we have stored data but IsLoggedIn is false, try to restore anyway (might be a preference issue)
        if (!isLoggedIn && !string.IsNullOrEmpty(authToken) && !string.IsNullOrEmpty(userDataJson))
        {
            LogMessage("SplashPage: Found stored auth token and user data but IsLoggedIn is false, attempting to validate");
            Console.WriteLine("SplashPage: Found stored auth token and user data but IsLoggedIn is false, attempting to validate");
            isLoggedIn = true; // Set to true so we attempt validation
        }
        
        if (isLoggedIn && !string.IsNullOrEmpty(authToken))
        {
            try
            {
                // Validate session with server
                var authService = App.Services.GetRequiredService<AuthService>();
                LogMessage($"SplashPage: Calling ValidateSessionAsync with AuthToken={(!string.IsNullOrEmpty(authToken) ? "present" : "missing")}");
                Console.WriteLine($"SplashPage: Calling ValidateSessionAsync with AuthToken={(!string.IsNullOrEmpty(authToken) ? "present" : "missing")}");
                var (sessionValid, user, message) = await authService.ValidateSessionAsync();
                LogMessage($"SplashPage: ValidateSessionAsync returned - Success={sessionValid}, HasUser={user != null}, Message={message}");
                Console.WriteLine($"SplashPage: ValidateSessionAsync returned - Success={sessionValid}, HasUser={user != null}, Message={message}");
                
                // Log the raw response for debugging
                if (!sessionValid)
                {
                    LogMessage($"SplashPage: Validation failed - will check if expired or network error. Message: '{message}'");
                    Console.WriteLine($"SplashPage: Validation failed - will check if expired or network error. Message: '{message}'");
                }
                
                if (sessionValid && user != null)
                {
                    App.CurrentUser = user;
                    // Ensure IsLoggedIn is set to true
                    Preferences.Set("IsLoggedIn", true);
                    LogMessage($"SplashPage: Session validated and restored, Email={user.Email}, Role={user.Role}");
                    Console.WriteLine($"SplashPage: Session validated and restored, Email={user.Email}, Role={user.Role}");
                    isLoggedIn = true;
                }
                else
                {
                    // Session validation failed - check if it's a genuine network error (can't reach server)
                    // vs. an invalid/expired session
                    // IMPORTANT: Only treat as network error if message explicitly says so AND doesn't mention expired/invalid
                    string lowerMessage = message.ToLowerInvariant();
                    bool isExpiredOrInvalid = lowerMessage.Contains("session expired") || 
                                             lowerMessage.Contains("invalid token") ||
                                             lowerMessage.Contains("expired") ||
                                             (lowerMessage.Contains("invalid") && !lowerMessage.Contains("response")) ||
                                             lowerMessage.Contains("unauthorized") ||
                                             lowerMessage.Contains("forbidden") ||
                                             lowerMessage.Contains("no authentication token");
                    
                    bool isNetworkError = !isExpiredOrInvalid && 
                                         (lowerMessage.Contains("network error") || 
                                          lowerMessage.Contains("failed to retrieve session token") ||
                                          lowerMessage.Contains("connection") ||
                                          lowerMessage.Contains("timeout") ||
                                          lowerMessage.Contains("dns") ||
                                          lowerMessage.Contains("name resolution"));
                    
                    if (isExpiredOrInvalid)
                    {
                        // Session is definitely expired/invalid - clear login state
                        LogMessage($"SplashPage: Session expired/invalid: {message}, clearing login state");
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
                        LogMessage($"SplashPage: Network error during validation (offline?), attempting to restore from stored data");
                        Console.WriteLine($"SplashPage: Network error during validation (offline?), attempting to restore from stored data");
                        if (!string.IsNullOrEmpty(userDataJson))
                        {
                            try
                            {
                                var storedUser = Newtonsoft.Json.JsonConvert.DeserializeObject<Models.User>(userDataJson);
                                if (storedUser != null)
                                {
                                    App.CurrentUser = storedUser;
                                    LogMessage($"SplashPage: Restored user from stored data (offline mode), Email={storedUser.Email}, Role={storedUser.Role}");
                                    Console.WriteLine($"SplashPage: Restored user from stored data (offline mode), Email={storedUser.Email}, Role={storedUser.Role}");
                                    isLoggedIn = true;
                                }
                            }
                            catch (Exception restoreEx)
                            {
                                LogMessage($"SplashPage: Failed to restore from stored data: {restoreEx.Message}");
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
                        // Unknown error - for now, restore from stored data to allow app to work
                        // This is a fallback - ideally validation should work, but if it doesn't, 
                        // we'll use stored data so the user can still use the app
                        LogMessage($"SplashPage: Unknown validation error: {message}, attempting to restore from stored data as fallback");
                        Console.WriteLine($"SplashPage: Unknown validation error: {message}, attempting to restore from stored data as fallback");
                        if (!string.IsNullOrEmpty(userDataJson) && !string.IsNullOrEmpty(authToken))
                        {
                            try
                            {
                                var storedUser = Newtonsoft.Json.JsonConvert.DeserializeObject<Models.User>(userDataJson);
                                if (storedUser != null)
                                {
                                    App.CurrentUser = storedUser;
                                    LogMessage($"SplashPage: Restored user from stored data (unknown error fallback), Email={storedUser.Email}, Role={storedUser.Role}");
                                    Console.WriteLine($"SplashPage: Restored user from stored data (unknown error fallback), Email={storedUser.Email}, Role={storedUser.Role}");
                                    // Keep IsLoggedIn as true since we have valid stored data
                                    Preferences.Set("IsLoggedIn", true);
                                    isLoggedIn = true;
                                }
                                else
                                {
                                    isLoggedIn = false;
                                }
                            }
                            catch (Exception restoreEx)
                            {
                                LogMessage($"SplashPage: Failed to restore from stored data: {restoreEx.Message}");
                                Console.WriteLine($"SplashPage: Failed to restore from stored data: {restoreEx.Message}");
                                isLoggedIn = false;
                            }
                        }
                        else
                        {
                            LogMessage($"SplashPage: No stored data or auth token available");
                            Console.WriteLine($"SplashPage: No stored data or auth token available");
                            isLoggedIn = false;
                        }
                        
                        // Only clear if we couldn't restore
                        if (!isLoggedIn)
                        {
                            LogMessage($"SplashPage: Could not restore from stored data, clearing login state");
                            Console.WriteLine($"SplashPage: Could not restore from stored data, clearing login state");
                            Preferences.Set("IsLoggedIn", false);
                            Preferences.Remove("AuthToken");
                            Preferences.Remove("UserData");
                            App.CurrentUser = null;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LogMessage($"SplashPage: Exception validating session: {ex.Message}, StackTrace: {ex.StackTrace}");
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
                    LogMessage($"SplashPage: Network exception, attempting offline restore");
                    Console.WriteLine($"SplashPage: Network exception, attempting offline restore");
                    if (!string.IsNullOrEmpty(userDataJson))
                    {
                        try
                        {
                            var storedUser = Newtonsoft.Json.JsonConvert.DeserializeObject<Models.User>(userDataJson);
                            if (storedUser != null)
                            {
                                App.CurrentUser = storedUser;
                                LogMessage($"SplashPage: Restored user from stored data (offline mode), Email={storedUser.Email}");
                                Console.WriteLine($"SplashPage: Restored user from stored data (offline mode), Email={storedUser.Email}");
                                isLoggedIn = true;
                            }
                        }
                        catch
                        {
                            LogMessage($"SplashPage: Failed to restore from stored data");
                            Console.WriteLine($"SplashPage: Failed to restore from stored data");
                        }
                    }
                }
                
                // If we couldn't restore or it wasn't a network error, clear login state
                if (!isLoggedIn)
                {
                    LogMessage($"SplashPage: Clearing login state due to exception");
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
            LogMessage($"SplashPage: Not logged in or no auth token - IsLoggedIn={isLoggedIn}, HasAuthToken={!string.IsNullOrEmpty(authToken)}");
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
            LogMessage($"SplashPage: Navigating to {targetRoute}, isLoggedIn={isLoggedIn}");
            Console.WriteLine($"SplashPage: Navigating to {targetRoute}");
            try
            {
                await Shell.Current.GoToAsync(targetRoute);
                LogMessage($"SplashPage: Navigation to {targetRoute} completed");
                Console.WriteLine($"SplashPage: Navigation to {targetRoute} completed");
            }
            catch (Exception navEx)
            {
                LogMessage($"SplashPage: Navigation error: {navEx.Message}, StackTrace: {navEx.StackTrace}");
                Console.WriteLine($"SplashPage: Navigation error: {navEx.Message}, StackTrace: {navEx.StackTrace}");
                // Fallback to login if navigation fails
                try
                {
                    await Shell.Current.GoToAsync("//Login");
                }
                catch
                {
                    LogMessage("SplashPage: Fallback navigation also failed");
                    Console.WriteLine("SplashPage: Fallback navigation also failed");
                }
            }
        }
        catch (Exception ex)
        {
            LogMessage($"SplashPage: Critical error in OnAppearing: {ex.Message}, StackTrace: {ex.StackTrace}");
            Console.WriteLine($"SplashPage: Critical error in OnAppearing: {ex.Message}, StackTrace: {ex.StackTrace}");
            // Try to navigate to login as fallback
            try
            {
                await Shell.Current.GoToAsync("//Login");
            }
            catch
            {
                LogMessage("SplashPage: Could not navigate to Login after error");
                Console.WriteLine("SplashPage: Could not navigate to Login after error");
            }
        }
    }
    
    private void LogMessage(string message)
    {
        try
        {
            // Try multiple locations to ensure we can write
            string? logPath = null;
            
            // Try cache directory first (more accessible)
            try
            {
                logPath = Path.Combine(FileSystem.CacheDirectory, "splash_page_log.txt");
            }
            catch
            {
                // Fallback to app data directory
                try
                {
                    logPath = Path.Combine(FileSystem.AppDataDirectory, "splash_page_log.txt");
                }
                catch
                {
                    // Last resort - try temp path
                    logPath = Path.Combine(Path.GetTempPath(), "splash_page_log.txt");
                }
            }
            
            var logEntry = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}\n";
            File.AppendAllText(logPath, logEntry);
            System.Diagnostics.Debug.WriteLine($"SPLASH LOG: {message}");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"SPLASH LOG ERROR: {ex.Message}");
        }
    }
}

