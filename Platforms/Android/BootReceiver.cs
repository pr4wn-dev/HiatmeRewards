using Android.App;
using Android.Content;
using Android.OS;
using Microsoft.Maui.Storage;

namespace HiatmeApp
{
    [BroadcastReceiver(Enabled = true, Exported = true, DirectBootAware = true)]
    [IntentFilter(new[] { Intent.ActionBootCompleted, Intent.ActionLockedBootCompleted })]
    public class BootReceiver : BroadcastReceiver
    {
        public override void OnReceive(Context? context, Intent? intent)
        {
            if (context == null) return;
            
            Console.WriteLine($"BootReceiver: Received intent {intent?.Action}");
            
            if (intent?.Action == Intent.ActionBootCompleted || 
                intent?.Action == Intent.ActionLockedBootCompleted)
            {
                // Check if user was logged in before reboot
                bool isLoggedIn = Preferences.Get("IsLoggedIn", false);
                string userRole = Preferences.Get("UserRole", string.Empty);
                
                Console.WriteLine($"BootReceiver: isLoggedIn={isLoggedIn}, userRole={userRole}");
                
                // Only restart location service for logged-in drivers/managers/owners
                if (isLoggedIn && !string.IsNullOrEmpty(userRole) && 
                    (userRole.Equals("Driver", StringComparison.OrdinalIgnoreCase) ||
                     userRole.Equals("Manager", StringComparison.OrdinalIgnoreCase) ||
                     userRole.Equals("Owner", StringComparison.OrdinalIgnoreCase)))
                {
                    Console.WriteLine("BootReceiver: Starting location service after boot");
                    
                    try
                    {
                        var serviceIntent = new Intent(context, typeof(LocationForegroundService));
                        
                        if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
                        {
                            context.StartForegroundService(serviceIntent);
                        }
                        else
                        {
                            context.StartService(serviceIntent);
                        }
                        
                        Console.WriteLine("BootReceiver: Location service started successfully");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"BootReceiver: Failed to start location service: {ex.Message}");
                    }
                }
                else
                {
                    Console.WriteLine("BootReceiver: Not starting location service - user not logged in or is client");
                }
            }
        }
    }
}
