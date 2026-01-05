using Microsoft.Maui.Controls;
using System.IO;
using System.Threading.Tasks;

namespace HiatMeApp;

public partial class ViewLogPage : ContentPage
{
    private string _currentLogPath = null;
    
    public ViewLogPage()
    {
        InitializeComponent();
        LoadLog();
    }
    
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

