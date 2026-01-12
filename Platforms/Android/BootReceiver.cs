using Android.App;
using Android.Content;

namespace HiatmeApp
{
    [BroadcastReceiver(Enabled = true, Exported = true, DirectBootAware = true)]
    [IntentFilter(new[] { Intent.ActionBootCompleted, Intent.ActionLockedBootCompleted })]
    public class BootReceiver : BroadcastReceiver
    {
        public override void OnReceive(Context? context, Intent? intent)
        {
            if (context == null) return;
            
            if (intent?.Action == Intent.ActionBootCompleted || 
                intent?.Action == Intent.ActionLockedBootCompleted)
            {
                System.Diagnostics.Debug.WriteLine("BootReceiver: Device booted, starting notification service");
                NotificationForegroundService.Start(context);
            }
        }
    }
}

