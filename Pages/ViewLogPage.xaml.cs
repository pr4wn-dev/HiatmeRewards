using Microsoft.Maui.Controls;
using System.IO;

namespace HiatMeApp;

public partial class ViewLogPage : ContentPage
{
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
}

