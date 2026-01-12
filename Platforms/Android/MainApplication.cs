using Android.App;
using Android.Runtime;
using HiatMeApp;
using Com.OneSignal.Android;

namespace HiatmeApp
{
    [Application]
    public class MainApplication : MauiApplication
    {
        // OneSignal App ID - same as website
        public const string OneSignalAppId = "1bb48e3c-7bce-4e46-9b4b-37983c1abbf2";
        
        public MainApplication(IntPtr handle, JniHandleOwnership ownership)
            : base(handle, ownership)
        {
        }

        public override void OnCreate()
        {
            base.OnCreate();
            
            // Initialize OneSignal
            OneSignal.InitWithContext(this, OneSignalAppId);
            
            System.Diagnostics.Debug.WriteLine("OneSignal: Initialized with App ID: " + OneSignalAppId);
        }

        protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();
        
        /// <summary>
        /// Get the current OneSignal subscription/player ID
        /// </summary>
        public static string? GetOneSignalPlayerId()
        {
            try
            {
                var user = OneSignal.User;
                if (user != null)
                {
                    var pushSubscription = user.PushSubscription;
                    if (pushSubscription != null)
                    {
                        var playerId = pushSubscription.Id;
                        System.Diagnostics.Debug.WriteLine($"OneSignal: Player ID = {playerId}");
                        return playerId;
                    }
                }
                System.Diagnostics.Debug.WriteLine("OneSignal: No player ID available yet");
                return null;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"OneSignal: Error getting player ID: {ex.Message}");
                return null;
            }
        }
        
        /// <summary>
        /// Request notification permission (Android 13+)
        /// </summary>
        public static void RequestNotificationPermission()
        {
            try
            {
                OneSignal.Notifications.RequestPermission(true, Continue.None);
                System.Diagnostics.Debug.WriteLine("OneSignal: Notification permission requested");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"OneSignal: Error requesting permission: {ex.Message}");
            }
        }
    }
}
