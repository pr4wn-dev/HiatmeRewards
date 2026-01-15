using Android.App;
using Android.Content;
using Android.OS;
using Android.Locations;
using Android.Provider;
using Android.Net;
using AndroidX.Core.App;
using Microsoft.Maui.Devices.Sensors;
using Microsoft.Maui.Storage;
using System.Net.Http;

namespace HiatmeApp
{
    [Service(
        ForegroundServiceType = Android.Content.PM.ForegroundService.TypeLocation,
        Exported = false,
        Name = "com.hiatme.app.LocationForegroundService"
    )]
    public class LocationForegroundService : Service, ILocationListener
    {
        private const int NotificationId = 8888;
        private const string ChannelId = "hiatme_location_channel";
        private const int UPDATE_INTERVAL_MS = 15000; // 15 seconds
        private const float MIN_DISTANCE_METERS = 10f;

        private LocationManager? _locationManager;
        private Handler? _handler;
        private HttpClient? _httpClient;
        private Microsoft.Maui.Devices.Sensors.Location? _lastLocation;
        private DateTime _lastUpdateTime = DateTime.MinValue;
        private bool _isRunning = false;

        public static bool IsRunning { get; private set; } = false;

        public override IBinder? OnBind(Intent? intent) => null;

        public override void OnCreate()
        {
            base.OnCreate();
            _httpClient = new HttpClient { BaseAddress = new System.Uri("https://hiatme.com") };
            _handler = new Handler(Looper.MainLooper!);
            Console.WriteLine("LocationForegroundService: Created");
        }

        public override StartCommandResult OnStartCommand(Intent? intent, StartCommandFlags flags, int startId)
        {
            if (_isRunning)
            {
                Console.WriteLine("LocationForegroundService: Already running");
                return StartCommandResult.Sticky;
            }

            CreateNotificationChannel();
            
            // Must call StartForeground within 5 seconds of starting service
            if (Build.VERSION.SdkInt >= BuildVersionCodes.Q)
            {
                StartForeground(NotificationId, CreateNotification(), Android.Content.PM.ForegroundService.TypeLocation);
            }
            else
            {
                StartForeground(NotificationId, CreateNotification());
            }

            StartLocationUpdates();
            _isRunning = true;
            IsRunning = true;
            
            // Request battery optimization exemption if not already granted
            // This will show a system dialog to the user
            RequestBatteryOptimizationExemption(this);
            
            Console.WriteLine("LocationForegroundService: Started");
            return StartCommandResult.Sticky;
        }

        public override void OnDestroy()
        {
            StopLocationUpdates();
            
            // Notify server that we're going offline before destroying
            try
            {
                Console.WriteLine("LocationForegroundService: Sending offline status before destroy");
                _ = SendOfflineStatusAsync("App closed");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"LocationForegroundService: Failed to send offline status: {ex.Message}");
            }
            
            _isRunning = false;
            IsRunning = false;
            _httpClient?.Dispose();
            Console.WriteLine("LocationForegroundService: Destroyed");
            base.OnDestroy();
        }

        public override void OnTaskRemoved(Intent? rootIntent)
        {
            Console.WriteLine("LocationForegroundService: Task removed - restarting service");
            
            // Restart the service when app is swiped away
            var restartIntent = new Intent(ApplicationContext, typeof(LocationForegroundService));
            restartIntent.SetPackage(PackageName);
            
            if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
            {
                ApplicationContext?.StartForegroundService(restartIntent);
            }
            else
            {
                ApplicationContext?.StartService(restartIntent);
            }
            
            base.OnTaskRemoved(rootIntent);
        }
        
        private async Task SendOfflineStatusAsync(string reason)
        {
            try
            {
                var authToken = Preferences.Get("AuthToken", string.Empty);
                if (string.IsNullOrEmpty(authToken))
                {
                    Console.WriteLine("LocationForegroundService: No auth token for offline status");
                    return;
                }

                if (_httpClient == null) return;

                var content = new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    { "auth_token", authToken },
                    { "is_online", "0" },
                    { "reason", reason }
                });

                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(3));
                var response = await _httpClient.PostAsync("/api/set_driver_status.php", content, cts.Token);
                Console.WriteLine($"LocationForegroundService: Offline status sent - {response.StatusCode}");
            }
            catch (TaskCanceledException)
            {
                Console.WriteLine("LocationForegroundService: Offline status request timed out");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"LocationForegroundService: Error sending offline status: {ex.Message}");
            }
        }

        private void CreateNotificationChannel()
        {
            if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
            {
                var channel = new NotificationChannel(
                    ChannelId,
                    "Location Tracking",
                    NotificationImportance.Low)
                {
                    Description = "Tracks your location for the live map"
                };

                var notificationManager = (NotificationManager?)GetSystemService(NotificationService);
                notificationManager?.CreateNotificationChannel(channel);
            }
        }

        private Notification CreateNotification()
        {
            // Create intent to open app when notification is tapped
            var intent = new Intent(this, typeof(MainActivity));
            intent.SetFlags(ActivityFlags.SingleTop);
            var pendingIntent = PendingIntent.GetActivity(this, 0, intent, PendingIntentFlags.Immutable);

            var builder = new NotificationCompat.Builder(this, ChannelId)
                .SetContentTitle("HiatMe")
                .SetContentText("Location tracking active")
                .SetSmallIcon(Android.Resource.Drawable.IcDialogMap)
                .SetOngoing(true)
                .SetPriority(NotificationCompat.PriorityLow)
                .SetContentIntent(pendingIntent);

            return builder.Build();
        }

        private void StartLocationUpdates()
        {
            try
            {
                _locationManager = (LocationManager?)GetSystemService(LocationService);
                if (_locationManager == null)
                {
                    Console.WriteLine("LocationForegroundService: LocationManager not available");
                    return;
                }

                // Try GPS first, fall back to network
                string provider = LocationManager.GpsProvider;
                if (!_locationManager.IsProviderEnabled(provider))
                {
                    provider = LocationManager.NetworkProvider;
                    if (!_locationManager.IsProviderEnabled(provider))
                    {
                        Console.WriteLine("LocationForegroundService: No location provider available");
                        return;
                    }
                }

                Console.WriteLine($"LocationForegroundService: Using provider '{provider}'");

                // Request location updates
                _locationManager.RequestLocationUpdates(
                    provider,
                    UPDATE_INTERVAL_MS,
                    MIN_DISTANCE_METERS,
                    this,
                    Looper.MainLooper
                );

                // Also try to get last known location immediately
                var lastKnown = _locationManager.GetLastKnownLocation(provider);
                if (lastKnown != null)
                {
                    Console.WriteLine($"LocationForegroundService: Got last known location - Lat: {lastKnown.Latitude}, Lng: {lastKnown.Longitude}");
                    _ = SendLocationToServerAsync(lastKnown.Latitude, lastKnown.Longitude, 
                        lastKnown.HasSpeed ? lastKnown.Speed * 2.23694 : null, // m/s to mph
                        lastKnown.HasBearing ? (double?)lastKnown.Bearing : null,
                        lastKnown.HasAccuracy ? (double?)lastKnown.Accuracy : null);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"LocationForegroundService: Error starting location updates: {ex.Message}");
            }
        }

        private void StopLocationUpdates()
        {
            try
            {
                _locationManager?.RemoveUpdates(this);
                Console.WriteLine("LocationForegroundService: Location updates stopped");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"LocationForegroundService: Error stopping location updates: {ex.Message}");
            }
        }

        // ILocationListener implementation
        public void OnLocationChanged(Android.Locations.Location location)
        {
            try
            {
                Console.WriteLine($"LocationForegroundService: Location changed - Lat: {location.Latitude:F6}, Lng: {location.Longitude:F6}");

                // Check if enough time has passed since last update
                var timeSinceLastUpdate = DateTime.Now - _lastUpdateTime;
                if (timeSinceLastUpdate.TotalSeconds < 10)
                {
                    Console.WriteLine("LocationForegroundService: Skipping update (too soon)");
                    return;
                }

                // Calculate speed in mph
                double? speedMph = null;
                if (location.HasSpeed)
                {
                    speedMph = location.Speed * 2.23694; // m/s to mph
                }

                // Get heading/bearing
                double? heading = location.HasBearing ? (double?)location.Bearing : null;

                // Get accuracy
                double? accuracy = location.HasAccuracy ? (double?)location.Accuracy : null;

                // Send to server
                _ = SendLocationToServerAsync(location.Latitude, location.Longitude, speedMph, heading, accuracy);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"LocationForegroundService: Error processing location: {ex.Message}");
            }
        }

        public void OnProviderDisabled(string provider)
        {
            Console.WriteLine($"LocationForegroundService: Provider '{provider}' disabled");
        }

        public void OnProviderEnabled(string provider)
        {
            Console.WriteLine($"LocationForegroundService: Provider '{provider}' enabled");
        }

        public void OnStatusChanged(string? provider, Availability status, Bundle? extras)
        {
            Console.WriteLine($"LocationForegroundService: Provider '{provider}' status changed to {status}");
        }

        private async Task SendLocationToServerAsync(double latitude, double longitude, double? speed, double? heading, double? accuracy)
        {
            try
            {
                var authToken = Preferences.Get("AuthToken", string.Empty);
                if (string.IsNullOrEmpty(authToken))
                {
                    Console.WriteLine("LocationForegroundService: No auth token available");
                    return;
                }

                if (_httpClient == null) return;

                var content = new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    { "auth_token", authToken },
                    { "latitude", latitude.ToString("F8") },
                    { "longitude", longitude.ToString("F8") },
                    { "speed", speed?.ToString("F2") ?? "" },
                    { "heading", heading?.ToString("F2") ?? "" },
                    { "accuracy", accuracy?.ToString("F2") ?? "" }
                });

                var response = await _httpClient.PostAsync("/api/update_location.php", content);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    _lastUpdateTime = DateTime.Now;
                    Console.WriteLine($"LocationForegroundService: Location sent successfully - Lat: {latitude:F6}, Lng: {longitude:F6}, Speed: {speed?.ToString("F1") ?? "N/A"} mph");
                }
                else
                {
                    Console.WriteLine($"LocationForegroundService: Failed to send location - {response.StatusCode}: {responseContent}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"LocationForegroundService: Error sending location: {ex.Message}");
            }
        }

        public static void Start(Context context)
        {
            var intent = new Intent(context, typeof(LocationForegroundService));
            if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
            {
                context.StartForegroundService(intent);
            }
            else
            {
                context.StartService(intent);
            }
            Console.WriteLine("LocationForegroundService: Start requested");
        }

        public static void Stop(Context context)
        {
            var intent = new Intent(context, typeof(LocationForegroundService));
            context.StopService(intent);
            IsRunning = false;
            Console.WriteLine("LocationForegroundService: Stop requested");
        }

        /// <summary>
        /// Check if the app is exempt from battery optimization
        /// </summary>
        public static bool IsIgnoringBatteryOptimizations(Context context)
        {
            if (Build.VERSION.SdkInt >= BuildVersionCodes.M)
            {
                var powerManager = (PowerManager?)context.GetSystemService(PowerService);
                if (powerManager != null && context.PackageName != null)
                {
                    return powerManager.IsIgnoringBatteryOptimizations(context.PackageName);
                }
            }
            return true; // Older versions don't have this restriction
        }

        /// <summary>
        /// Request battery optimization exemption from the user.
        /// This will show a system dialog asking the user to allow unrestricted battery usage.
        /// </summary>
        public static void RequestBatteryOptimizationExemption(Context context)
        {
            if (Build.VERSION.SdkInt >= BuildVersionCodes.M)
            {
                if (!IsIgnoringBatteryOptimizations(context))
                {
                    Console.WriteLine("LocationForegroundService: Requesting battery optimization exemption");
                    try
                    {
                        var intent = new Intent(Settings.ActionRequestIgnoreBatteryOptimizations);
                        intent.SetData(Android.Net.Uri.Parse($"package:{context.PackageName}"));
                        intent.AddFlags(ActivityFlags.NewTask);
                        context.StartActivity(intent);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"LocationForegroundService: Failed to request battery exemption: {ex.Message}");
                        // Fallback: open battery optimization settings
                        try
                        {
                            var fallbackIntent = new Intent(Settings.ActionIgnoreBatteryOptimizationSettings);
                            fallbackIntent.AddFlags(ActivityFlags.NewTask);
                            context.StartActivity(fallbackIntent);
                        }
                        catch (Exception ex2)
                        {
                            Console.WriteLine($"LocationForegroundService: Fallback also failed: {ex2.Message}");
                        }
                    }
                }
                else
                {
                    Console.WriteLine("LocationForegroundService: Already exempt from battery optimization");
                }
            }
        }
    }
}

