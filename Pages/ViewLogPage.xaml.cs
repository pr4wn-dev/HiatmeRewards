using HiatMeApp.Models;
using HiatMeApp.Services;
using HiatMeApp.ViewModels;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Storage;
using Newtonsoft.Json;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace HiatMeApp;

public partial class ViewLogPage : ContentPage
{
    private string _currentLogPath = null;
    
    public ViewLogPage()
    {
        InitializeComponent();
        BindingContext = App.Services?.GetService<ViewLogViewModel>() ?? new ViewLogViewModel();
        LoadLog();
    }
    
    protected override void OnAppearing()
    {
        base.OnAppearing();
        LoadLog(); // Refresh log when page appears
    }
    
    #region Debug Tools
    
    /// <summary>
    /// Simulates the OS tombstoning the app by clearing in-memory state,
    /// then restoring from Preferences to verify state restoration works.
    /// </summary>
    private async void OnSimulateTombstoneClicked(object sender, EventArgs e)
    {
        try
        {
            var sb = new StringBuilder();
            sb.AppendLine("=== TOMBSTONE SIMULATION ===");
            sb.AppendLine($"Time: {DateTime.Now:HH:mm:ss}");
            sb.AppendLine();
            
            // Show current state before
            sb.AppendLine("BEFORE TOMBSTONE:");
            sb.AppendLine($"  App.CurrentUser: {(App.CurrentUser != null ? App.CurrentUser.Email : "NULL")}");
            sb.AppendLine($"  IsLoggedIn: {Preferences.Get("IsLoggedIn", false)}");
            sb.AppendLine($"  HasAuthToken: {!string.IsNullOrEmpty(Preferences.Get("AuthToken", null))}");
            sb.AppendLine($"  HasUserData: {!string.IsNullOrEmpty(Preferences.Get("UserData", string.Empty))}");
            sb.AppendLine();
            
            // Simulate tombstone - clear in-memory state
            sb.AppendLine("SIMULATING TOMBSTONE...");
            sb.AppendLine("  Clearing App.CurrentUser (simulating process kill)");
            App.CurrentUser = null;
            sb.AppendLine("  App.CurrentUser is now NULL");
            sb.AppendLine();
            
            // Now restore - this is what happens when app resumes
            sb.AppendLine("RESTORING STATE...");
            if (Preferences.Get("IsLoggedIn", false))
            {
                var userJson = Preferences.Get("UserData", string.Empty);
                if (!string.IsNullOrEmpty(userJson))
                {
                    var user = JsonConvert.DeserializeObject<User>(userJson);
                    if (user != null)
                    {
                        App.CurrentUser = user;
                        sb.AppendLine($"  ✅ Restored CurrentUser from Preferences: {user.Email}");
                        
                        // Update menu
                        if (Shell.Current?.BindingContext is AppShellViewModel vm)
                        {
                            vm.UpdateMenuItems();
                            sb.AppendLine("  ✅ Updated shell menu items");
                        }
                    }
                    else
                    {
                        sb.AppendLine("  ❌ Failed to deserialize user from Preferences");
                    }
                }
                else
                {
                    sb.AppendLine("  ❌ No UserData in Preferences to restore from");
                }
            }
            else
            {
                sb.AppendLine("  ⚠️ User was not logged in, nothing to restore");
            }
            
            sb.AppendLine();
            sb.AppendLine("AFTER RESTORE:");
            sb.AppendLine($"  App.CurrentUser: {(App.CurrentUser != null ? App.CurrentUser.Email : "NULL")}");
            sb.AppendLine();
            sb.AppendLine("=== SIMULATION COMPLETE ===");
            
            ShowDebugOutput(sb.ToString());
            ShowStatus("Tombstone simulation complete", true);
        }
        catch (Exception ex)
        {
            ShowDebugOutput($"ERROR: {ex.Message}\n{ex.StackTrace}");
            ShowStatus($"Error: {ex.Message}", false);
        }
    }
    
    /// <summary>
    /// Forces a session revalidation with the server.
    /// This is what happens when the app resumes after being backgrounded for a while.
    /// </summary>
    private async void OnForceRevalidateClicked(object sender, EventArgs e)
    {
        try
        {
            var sb = new StringBuilder();
            sb.AppendLine("=== FORCE SESSION REVALIDATION ===");
            sb.AppendLine($"Time: {DateTime.Now:HH:mm:ss}");
            sb.AppendLine();
            
            if (!Preferences.Get("IsLoggedIn", false))
            {
                sb.AppendLine("⚠️ Not logged in - nothing to revalidate");
                ShowDebugOutput(sb.ToString());
                ShowStatus("Not logged in", false);
                return;
            }
            
            var authToken = Preferences.Get("AuthToken", null);
            if (string.IsNullOrEmpty(authToken))
            {
                sb.AppendLine("❌ No auth token found");
                ShowDebugOutput(sb.ToString());
                ShowStatus("No auth token", false);
                return;
            }
            
            sb.AppendLine("Calling ValidateSessionAsync...");
            ShowDebugOutput(sb.ToString());
            
            var authService = App.Services.GetRequiredService<AuthService>();
            var (success, user, message) = await authService.ValidateSessionAsync();
            
            sb.AppendLine($"Result: {(success ? "SUCCESS" : "FAILED")}");
            sb.AppendLine($"Message: {message}");
            
            if (success && user != null)
            {
                sb.AppendLine($"✅ Session valid for: {user.Email}");
                App.CurrentUser = user;
                
                if (Shell.Current?.BindingContext is AppShellViewModel vm)
                {
                    vm.UpdateMenuItems();
                    sb.AppendLine("✅ Updated shell menu");
                }
            }
            else
            {
                sb.AppendLine($"❌ Session invalid: {message}");
                
                if (message.StartsWith("LOGGED_IN_ELSEWHERE:", StringComparison.OrdinalIgnoreCase))
                {
                    sb.AppendLine("⚠️ User was logged in elsewhere!");
                }
            }
            
            sb.AppendLine();
            sb.AppendLine("=== REVALIDATION COMPLETE ===");
            ShowDebugOutput(sb.ToString());
            ShowStatus(success ? "Session valid" : "Session invalid", success);
        }
        catch (Exception ex)
        {
            ShowDebugOutput($"ERROR: {ex.Message}\n{ex.StackTrace}");
            ShowStatus($"Error: {ex.Message}", false);
        }
    }
    
    /// <summary>
    /// Shows the current app state including user data, preferences, tokens, etc.
    /// </summary>
    private void OnShowStateClicked(object sender, EventArgs e)
    {
        try
        {
            var sb = new StringBuilder();
            sb.AppendLine("=== CURRENT APP STATE ===");
            sb.AppendLine($"Time: {DateTime.Now:HH:mm:ss}");
            sb.AppendLine();
            
            // In-memory state
            sb.AppendLine("IN-MEMORY STATE:");
            if (App.CurrentUser != null)
            {
                sb.AppendLine($"  CurrentUser.Email: {App.CurrentUser.Email}");
                sb.AppendLine($"  CurrentUser.Name: {App.CurrentUser.Name}");
                sb.AppendLine($"  CurrentUser.Role: {App.CurrentUser.Role}");
                sb.AppendLine($"  CurrentUser.UserId: {App.CurrentUser.UserId}");
            }
            else
            {
                sb.AppendLine("  CurrentUser: NULL");
            }
            sb.AppendLine();
            
            // Preferences
            sb.AppendLine("PREFERENCES:");
            sb.AppendLine($"  IsLoggedIn: {Preferences.Get("IsLoggedIn", false)}");
            sb.AppendLine($"  UserEmail: {Preferences.Get("UserEmail", "(not set)")}");
            
            var authToken = Preferences.Get("AuthToken", null);
            if (!string.IsNullOrEmpty(authToken))
            {
                sb.AppendLine($"  AuthToken: {authToken.Substring(0, Math.Min(20, authToken.Length))}...");
            }
            else
            {
                sb.AppendLine("  AuthToken: (not set)");
            }
            
            var csrfToken = Preferences.Get("CSRFToken", null);
            if (!string.IsNullOrEmpty(csrfToken))
            {
                sb.AppendLine($"  CSRFToken: {csrfToken.Substring(0, Math.Min(20, csrfToken.Length))}...");
            }
            else
            {
                sb.AppendLine("  CSRFToken: (not set)");
            }
            
            var lastRoute = Preferences.Get("LastRoute", "(not set)");
            sb.AppendLine($"  LastRoute: {lastRoute}");
            
            var lastApiCall = Preferences.Get("LastSuccessfulApiCall", "(not set)");
            sb.AppendLine($"  LastSuccessfulApiCall: {lastApiCall}");
            sb.AppendLine();
            
            // UserData
            sb.AppendLine("STORED USER DATA:");
            var userJson = Preferences.Get("UserData", string.Empty);
            if (!string.IsNullOrEmpty(userJson))
            {
                try
                {
                    var user = JsonConvert.DeserializeObject<User>(userJson);
                    if (user != null)
                    {
                        sb.AppendLine($"  Email: {user.Email}");
                        sb.AppendLine($"  Name: {user.Name}");
                        sb.AppendLine($"  Role: {user.Role}");
                    }
                }
                catch
                {
                    sb.AppendLine("  (Error parsing UserData)");
                }
            }
            else
            {
                sb.AppendLine("  (No UserData stored)");
            }
            sb.AppendLine();
            
            // Shell state
            sb.AppendLine("SHELL STATE:");
            if (Shell.Current != null)
            {
                sb.AppendLine($"  CurrentRoute: {Shell.Current.CurrentState?.Location}");
                sb.AppendLine($"  FlyoutIsPresented: {Shell.Current.FlyoutIsPresented}");
            }
            else
            {
                sb.AppendLine("  Shell.Current: NULL");
            }
            
            sb.AppendLine();
            sb.AppendLine("=== END STATE ===");
            
            ShowDebugOutput(sb.ToString());
            ShowStatus("State retrieved", true);
        }
        catch (Exception ex)
        {
            ShowDebugOutput($"ERROR: {ex.Message}\n{ex.StackTrace}");
            ShowStatus($"Error: {ex.Message}", false);
        }
    }
    
    /// <summary>
    /// Checks the current session status with the server without modifying state.
    /// </summary>
    private async void OnCheckSessionClicked(object sender, EventArgs e)
    {
        try
        {
            var sb = new StringBuilder();
            sb.AppendLine("=== SESSION CHECK ===");
            sb.AppendLine($"Time: {DateTime.Now:HH:mm:ss}");
            sb.AppendLine();
            
            sb.AppendLine("Checking session with server...");
            ShowDebugOutput(sb.ToString());
            
            var authService = App.Services.GetRequiredService<AuthService>();
            var (success, user, message) = await authService.ValidateSessionAsync();
            
            sb.AppendLine($"Success: {success}");
            sb.AppendLine($"Message: {message}");
            
            if (user != null)
            {
                sb.AppendLine($"User: {user.Email} ({user.Role})");
            }
            
            if (message.StartsWith("LOGGED_IN_ELSEWHERE:", StringComparison.OrdinalIgnoreCase))
            {
                sb.AppendLine();
                sb.AppendLine("⚠️ LOGGED IN ELSEWHERE DETECTED!");
                sb.AppendLine("The AuthService should have shown a popup.");
            }
            
            sb.AppendLine();
            sb.AppendLine("=== CHECK COMPLETE ===");
            ShowDebugOutput(sb.ToString());
            ShowStatus(success ? "Session active" : "Session invalid", success);
        }
        catch (Exception ex)
        {
            ShowDebugOutput($"ERROR: {ex.Message}\n{ex.StackTrace}");
            ShowStatus($"Error: {ex.Message}", false);
        }
    }
    
    /// <summary>
    /// Clears all auth data and logs user out (for testing login flow).
    /// </summary>
    private async void OnClearAuthClicked(object sender, EventArgs e)
    {
        try
        {
            bool confirm = await DisplayAlert(
                "Clear Auth Data", 
                "This will log you out and clear all authentication data. Continue?", 
                "Yes", "No");
            
            if (!confirm) return;
            
            var sb = new StringBuilder();
            sb.AppendLine("=== CLEARING AUTH DATA ===");
            sb.AppendLine($"Time: {DateTime.Now:HH:mm:ss}");
            sb.AppendLine();
            
            // Clear preferences
            Preferences.Set("IsLoggedIn", false);
            Preferences.Remove("AuthToken");
            Preferences.Remove("UserData");
            Preferences.Remove("CSRFToken");
            Preferences.Remove("LastSuccessfulApiCall");
            sb.AppendLine("✅ Cleared Preferences");
            
            // Clear in-memory
            App.CurrentUser = null;
            sb.AppendLine("✅ Cleared App.CurrentUser");
            
            // Update menu
            if (Shell.Current?.BindingContext is AppShellViewModel vm)
            {
                vm.UpdateMenuItems();
                sb.AppendLine("✅ Updated shell menu");
            }
            
            if (Shell.Current is AppShell appShell)
            {
                appShell.UpdateMenuVisibility();
                sb.AppendLine("✅ Updated menu visibility");
            }
            
            sb.AppendLine();
            sb.AppendLine("=== AUTH CLEARED ===");
            sb.AppendLine("Navigating to Login page...");
            
            ShowDebugOutput(sb.ToString());
            ShowStatus("Auth cleared - logging out", true);
            
            await Task.Delay(1000); // Brief pause to show the message
            await Shell.Current.GoToAsync("//Login");
        }
        catch (Exception ex)
        {
            ShowDebugOutput($"ERROR: {ex.Message}\n{ex.StackTrace}");
            ShowStatus($"Error: {ex.Message}", false);
        }
    }
    
    private void ShowDebugOutput(string text)
    {
        DebugOutputFrame.IsVisible = true;
        DebugOutput.Text = text;
    }
    
    #endregion
    
    #region Log Functions
    
    private void LoadLog()
    {
        try
        {
            string logContent = "=== VEHICLE PAGE LOG ===\n\n";
            bool foundVehicle = false;
            
            // Try to load vehicle page log
            try
            {
                var logPath = Path.Combine(FileSystem.CacheDirectory, "vehicle_page_log.txt");
                if (File.Exists(logPath))
                {
                    logContent += File.ReadAllText(logPath);
                    foundVehicle = true;
                }
            }
            catch { }
            
            if (!foundVehicle)
            {
                try
                {
                    var logPath = Path.Combine(FileSystem.AppDataDirectory, "vehicle_page_log.txt");
                    if (File.Exists(logPath))
                    {
                        logContent += File.ReadAllText(logPath);
                        foundVehicle = true;
                    }
                }
                catch { }
            }
            
            if (!foundVehicle)
            {
                logContent += "No vehicle page log found.\n";
            }
            
            logContent += "\n\n=== SPLASH PAGE LOG ===\n\n";
            bool foundSplash = false;
            
            // Try to load splash page log
            try
            {
                var logPath = Path.Combine(FileSystem.CacheDirectory, "splash_page_log.txt");
                if (File.Exists(logPath))
                {
                    logContent += File.ReadAllText(logPath);
                    _currentLogPath = logPath;
                    foundSplash = true;
                }
            }
            catch { }
            
            if (!foundSplash)
            {
                try
                {
                    var logPath = Path.Combine(FileSystem.AppDataDirectory, "splash_page_log.txt");
                    if (File.Exists(logPath))
                    {
                        logContent += File.ReadAllText(logPath);
                        _currentLogPath = logPath;
                        foundSplash = true;
                    }
                }
                catch { }
            }
            
            if (!foundSplash)
            {
                logContent += "No splash page log found.";
            }
            
            logContent += "\n\n=== AUTH SERVICE LOG ===\n\n";
            bool foundAuth = false;
            
            // Try to load auth service log
            try
            {
                var logPath = Path.Combine(FileSystem.CacheDirectory, "auth_service_log.txt");
                if (File.Exists(logPath))
                {
                    logContent += File.ReadAllText(logPath);
                    _currentLogPath = logPath;
                    foundAuth = true;
                }
            }
            catch { }
            
            if (!foundAuth)
            {
                try
                {
                    var logPath = Path.Combine(FileSystem.AppDataDirectory, "auth_service_log.txt");
                    if (File.Exists(logPath))
                    {
                        logContent += File.ReadAllText(logPath);
                        _currentLogPath = logPath;
                        foundAuth = true;
                    }
                }
                catch { }
            }
            
            if (!foundAuth)
            {
                logContent += "No auth service log found.";
            }
            
            LogEditor.Text = logContent;
        }
        catch (Exception ex)
        {
            LogEditor.Text = $"Error loading log: {ex.Message}\n{ex.StackTrace}";
        }
    }
    
    private async void OnCopyClicked(object sender, EventArgs e)
    {
        try
        {
            var logText = LogEditor.Text;
            if (string.IsNullOrEmpty(logText))
            {
                ShowStatus("No log content to copy", false);
                return;
            }
            
            await Clipboard.SetTextAsync(logText);
            ShowStatus("Log copied to clipboard!", true);
        }
        catch (Exception ex)
        {
            ShowStatus($"Failed to copy: {ex.Message}", false);
        }
    }
    
    private async void OnClearClicked(object sender, EventArgs e)
    {
        try
        {
            bool confirmed = await DisplayAlert("Clear Log", "Are you sure you want to clear all log files?", "Yes", "No");
            if (!confirmed)
                return;
            
            bool cleared = false;
            
            // Clear vehicle page log
            try
            {
                var logPath = Path.Combine(FileSystem.CacheDirectory, "vehicle_page_log.txt");
                if (File.Exists(logPath))
                {
                    File.WriteAllText(logPath, string.Empty);
                    cleared = true;
                }
            }
            catch { }
            
            try
            {
                var logPath = Path.Combine(FileSystem.AppDataDirectory, "vehicle_page_log.txt");
                if (File.Exists(logPath))
                {
                    File.WriteAllText(logPath, string.Empty);
                    cleared = true;
                }
            }
            catch { }
            
            // Clear splash page log
            try
            {
                var logPath = Path.Combine(FileSystem.CacheDirectory, "splash_page_log.txt");
                if (File.Exists(logPath))
                {
                    File.WriteAllText(logPath, string.Empty);
                    cleared = true;
                }
            }
            catch { }
            
            try
            {
                var logPath = Path.Combine(FileSystem.AppDataDirectory, "splash_page_log.txt");
                if (File.Exists(logPath))
                {
                    File.WriteAllText(logPath, string.Empty);
                    cleared = true;
                }
            }
            catch { }
            
            // Clear auth service log
            try
            {
                var logPath = Path.Combine(FileSystem.CacheDirectory, "auth_service_log.txt");
                if (File.Exists(logPath))
                {
                    File.WriteAllText(logPath, string.Empty);
                    cleared = true;
                }
            }
            catch { }
            
            try
            {
                var logPath = Path.Combine(FileSystem.AppDataDirectory, "auth_service_log.txt");
                if (File.Exists(logPath))
                {
                    File.WriteAllText(logPath, string.Empty);
                    cleared = true;
                }
            }
            catch { }
            
            if (cleared)
            {
                LogEditor.Text = "Logs cleared.";
                ShowStatus("Logs cleared successfully!", true);
            }
            else
            {
                ShowStatus("No log files found to clear", false);
            }
        }
        catch (Exception ex)
        {
            ShowStatus($"Failed to clear log: {ex.Message}", false);
        }
    }
    
    #endregion
    
    private void ShowStatus(string message, bool isSuccess)
    {
        StatusLabel.Text = message;
        StatusLabel.TextColor = isSuccess ? Color.FromArgb("#28a745") : Color.FromArgb("#dc3545");
        
        // Clear status after 3 seconds
        _ = Task.Run(async () =>
        {
            await Task.Delay(3000);
            await Microsoft.Maui.ApplicationModel.MainThread.InvokeOnMainThreadAsync(() =>
            {
                StatusLabel.Text = "";
            });
        });
    }
}
