using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using Android.Provider;
using AndroidX.Core.App;
using AndroidX.Core.Content;

namespace HiatmeApp
{
    [Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, LaunchMode = LaunchMode.SingleTop, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
    public class MainActivity : MauiAppCompatActivity
    {
        protected override void OnCreate(Bundle? savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            
            // Request battery optimization exemption for reliable notifications
            RequestBatteryOptimizationExemption();
        }
        
        private void RequestBatteryOptimizationExemption()
        {
            try
            {
                if (Build.VERSION.SdkInt >= BuildVersionCodes.M)
                {
                    var packageName = PackageName;
                    var pm = (PowerManager?)GetSystemService(PowerService);
                    
                    if (pm != null && !pm.IsIgnoringBatteryOptimizations(packageName))
                    {
                        // Show system dialog to request exemption
                        var intent = new Intent(Settings.ActionRequestIgnoreBatteryOptimizations);
                        intent.SetData(Android.Net.Uri.Parse($"package:{packageName}"));
                        StartActivity(intent);
                        
                        System.Diagnostics.Debug.WriteLine("Battery: Requested optimization exemption");
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine("Battery: Already exempt from optimization");
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Battery: Error requesting exemption: {ex.Message}");
            }
        }
    }
}
