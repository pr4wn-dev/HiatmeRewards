using Android.App;
using Android.Runtime;
using HiatMeApp;
using OneSignalSDK.DotNet;

namespace HiatmeApp
{
    [Application]
    public class MainApplication : MauiApplication
    {
        // OneSignal App ID - same as website
        public const string OneSignalAppId = "1bb48e3c-7bce-4e46-9b4b-37983c1abbf2";
        
        // Store the subscription ID when it becomes available
        private static string? _cachedPlayerId = null;
        private static bool _subscriptionObserverAdded = false;
        
        public MainApplication(IntPtr handle, JniHandleOwnership ownership)
            : base(handle, ownership)
        {
        }

        public override void OnCreate()
        {
            base.OnCreate();
            
            // Initialize OneSignal SDK v5
            OneSignal.Initialize(OneSignalAppId);
            
            // Add subscription observer to catch when ID becomes available
            if (!_subscriptionObserverAdded)
            {
                OneSignal.User.PushSubscription.Changed += OnSubscriptionChanged;
                _subscriptionObserverAdded = true;
            }
            
            // Request notification permission (Android 13+)
            OneSignal.Notifications.RequestPermissionAsync(true);
            
            // Try to get existing subscription ID
            try
            {
                _cachedPlayerId = OneSignal.User.PushSubscription.Id;
                System.Diagnostics.Debug.WriteLine($"OneSignal: Initial Player ID = {_cachedPlayerId ?? "null"}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"OneSignal: Error getting initial player ID: {ex.Message}");
            }
            
            System.Diagnostics.Debug.WriteLine("OneSignal: Initialized with App ID: " + OneSignalAppId);
        }
        
        private static void OnSubscriptionChanged(object? sender, OneSignalSDK.DotNet.Core.User.Subscriptions.PushSubscriptionChangedEventArgs e)
        {
            try
            {
                var newId = e.State.Current.Id;
                System.Diagnostics.Debug.WriteLine($"OneSignal: Subscription changed - New ID = {newId ?? "null"}");
                
                if (!string.IsNullOrEmpty(newId))
                {
                    _cachedPlayerId = newId;
                    
                    // If user is logged in, save the player ID
                    var authToken = Microsoft.Maui.Storage.Preferences.Get("AuthToken", null);
                    if (!string.IsNullOrEmpty(authToken))
                    {
                        System.Diagnostics.Debug.WriteLine($"OneSignal: User logged in, will save player ID: {newId}");
                        _ = SavePlayerIdToServerAsync(newId);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"OneSignal: Error in subscription changed handler: {ex.Message}");
            }
        }
        
        private static async Task SavePlayerIdToServerAsync(string playerId)
        {
            try
            {
                // Get auth service from the app
                var authService = App.Services?.GetService<HiatMeApp.Services.AuthService>();
                if (authService != null)
                {
                    var result = await authService.SaveOneSignalPlayerIdAsync(playerId);
                    System.Diagnostics.Debug.WriteLine($"OneSignal: Save player ID result: {result.success}, {result.message}");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"OneSignal: Error saving player ID: {ex.Message}");
            }
        }

        protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();
        
        /// <summary>
        /// Get the current OneSignal subscription/player ID
        /// </summary>
        public static string? GetOneSignalPlayerId()
        {
            try
            {
                // First check cached value
                if (!string.IsNullOrEmpty(_cachedPlayerId))
                {
                    System.Diagnostics.Debug.WriteLine($"OneSignal: Returning cached Player ID = {_cachedPlayerId}");
                    return _cachedPlayerId;
                }
                
                // Try to get fresh value
                var subscriptionId = OneSignal.User.PushSubscription.Id;
                System.Diagnostics.Debug.WriteLine($"OneSignal: Player ID = {subscriptionId ?? "null"}");
                
                if (!string.IsNullOrEmpty(subscriptionId))
                {
                    _cachedPlayerId = subscriptionId;
                }
                
                return subscriptionId;
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
        public static async void RequestNotificationPermission()
        {
            try
            {
                await OneSignal.Notifications.RequestPermissionAsync(true);
                System.Diagnostics.Debug.WriteLine("OneSignal: Notification permission requested");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"OneSignal: Error requesting permission: {ex.Message}");
            }
        }
    }
}
