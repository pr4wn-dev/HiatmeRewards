using HiatMeApp.ViewModels;
using Microsoft.Maui.Controls;
using System.IO;

namespace HiatMeApp;

public partial class HomePage : ContentPage
{
    public HomePage(HomeViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
    
    private async void OnViewLogClicked(object sender, EventArgs e)
    {
        try
        {
            string logPath = null;
            string logContent = "Log file not found. Tried:\n";
            
            // Try cache directory
            try
            {
                logPath = Path.Combine(FileSystem.CacheDirectory, "vehicle_page_log.txt");
                if (File.Exists(logPath))
                {
                    logContent = File.ReadAllText(logPath);
                    await DisplayAlert("Vehicle Page Log", $"Path: {logPath}\n\n{logContent}", "OK");
                    return;
                }
                logContent += $"- {logPath} (not found)\n";
            }
            catch { }
            
            // Try app data directory
            try
            {
                logPath = Path.Combine(FileSystem.AppDataDirectory, "vehicle_page_log.txt");
                if (File.Exists(logPath))
                {
                    logContent = File.ReadAllText(logPath);
                    await DisplayAlert("Vehicle Page Log", $"Path: {logPath}\n\n{logContent}", "OK");
                    return;
                }
                logContent += $"- {logPath} (not found)\n";
            }
            catch { }
            
            // Try temp path
            try
            {
                logPath = Path.Combine(Path.GetTempPath(), "vehicle_page_log.txt");
                if (File.Exists(logPath))
                {
                    logContent = File.ReadAllText(logPath);
                    await DisplayAlert("Vehicle Page Log", $"Path: {logPath}\n\n{logContent}", "OK");
                    return;
                }
                logContent += $"- {logPath} (not found)\n";
            }
            catch { }
            
            await DisplayAlert("Vehicle Page Log", logContent, "OK");
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Could not read log: {ex.Message}", "OK");
        }
    }
}