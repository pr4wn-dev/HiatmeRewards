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
            string logContent = "Log file not found. Tried:\n\n";
            bool found = false;
            
            // Try cache directory
            try
            {
                var logPath = Path.Combine(FileSystem.CacheDirectory, "vehicle_page_log.txt");
                if (File.Exists(logPath))
                {
                    logContent = File.ReadAllText(logPath);
                    _currentLogPath = logPath;
                    found = true;
                }
                else
                {
                    logContent += $"- {logPath} (not found)\n";
                }
            }
            catch (Exception ex)
            {
                logContent += $"Cache directory error: {ex.Message}\n";
            }
            
            // Try app data directory
            if (!found)
            {
                try
                {
                    var logPath = Path.Combine(FileSystem.AppDataDirectory, "vehicle_page_log.txt");
                    if (File.Exists(logPath))
                    {
                        logContent = File.ReadAllText(logPath);
                        _currentLogPath = logPath;
                        found = true;
                    }
                    else
                    {
                        logContent += $"- {logPath} (not found)\n";
                    }
                }
                catch (Exception ex)
                {
                    logContent += $"App data directory error: {ex.Message}\n";
                }
            }
            
            // Try temp path
            if (!found)
            {
                try
                {
                    var logPath = Path.Combine(Path.GetTempPath(), "vehicle_page_log.txt");
                    if (File.Exists(logPath))
                    {
                        logContent = File.ReadAllText(logPath);
                        _currentLogPath = logPath;
                        found = true;
                    }
                    else
                    {
                        logContent += $"- {logPath} (not found)\n";
                    }
                }
                catch (Exception ex)
                {
                    logContent += $"Temp path error: {ex.Message}\n";
                }
            }
            
            if (!found)
            {
                logContent += "\nNo log file found. The Vehicle page may not have been accessed yet, or logging failed.";
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
            bool confirmed = await DisplayAlert("Clear Log", "Are you sure you want to clear the log file?", "Yes", "No");
            if (!confirmed)
                return;
            
            if (!string.IsNullOrEmpty(_currentLogPath) && File.Exists(_currentLogPath))
            {
                File.WriteAllText(_currentLogPath, string.Empty);
                LogEditor.Text = "Log cleared.";
                ShowStatus("Log cleared successfully!", true);
            }
            else
            {
                // Try to clear from all possible locations
                bool cleared = false;
                
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
                
                try
                {
                    var logPath = Path.Combine(Path.GetTempPath(), "vehicle_page_log.txt");
                    if (File.Exists(logPath))
                    {
                        File.WriteAllText(logPath, string.Empty);
                        cleared = true;
                    }
                }
                catch { }
                
                if (cleared)
                {
                    LogEditor.Text = "Log cleared.";
                    ShowStatus("Log cleared successfully!", true);
                }
                else
                {
                    ShowStatus("No log file found to clear", false);
                }
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

