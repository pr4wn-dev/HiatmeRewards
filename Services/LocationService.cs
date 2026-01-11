using Microsoft.Maui.Devices.Sensors;
using Microsoft.Maui.Storage;
using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace HiatMeApp.Services
{
    /// <summary>
    /// Handles GPS location tracking and sends updates to the server.
    /// Only tracks for Driver, Manager, and Owner roles.
    /// </summary>
    public class LocationService
    {
        private readonly HttpClient _httpClient;
        private CancellationTokenSource? _trackingCts;
        private bool _isTracking = false;
        private Location? _lastLocation;
        private DateTime _lastUpdateTime = DateTime.MinValue;
        
        // Update interval in seconds
        private const int UPDATE_INTERVAL_SECONDS = 15;
        
        // Minimum distance change (in meters) to send update
        private const double MIN_DISTANCE_CHANGE_METERS = 10;
        
        // Roles that should be tracked
        private static readonly string[] TrackedRoles = { "Driver", "Manager", "Owner" };

        public bool IsTracking => _isTracking;

        public LocationService(HttpClient httpClient)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            Console.WriteLine("LocationService initialized");
        }

        /// <summary>
        /// Start tracking location for eligible roles
        /// </summary>
        public async Task StartTrackingAsync(string role)
        {
            if (_isTracking)
            {
                Console.WriteLine("LocationService: Already tracking");
                return;
            }

            // Only track for Driver, Manager, Owner
            if (!Array.Exists(TrackedRoles, r => r.Equals(role, StringComparison.OrdinalIgnoreCase)))
            {
                Console.WriteLine($"LocationService: Role '{role}' is not tracked");
                return;
            }

            // Check and request location permission
            var permissionStatus = await CheckAndRequestLocationPermission();
            if (permissionStatus != PermissionStatus.Granted)
            {
                Console.WriteLine("LocationService: Location permission not granted");
                return;
            }

            _isTracking = true;
            _trackingCts = new CancellationTokenSource();
            
            Console.WriteLine($"LocationService: Starting tracking for role '{role}'");

            // Start the tracking loop in background
            _ = Task.Run(() => TrackingLoopAsync(_trackingCts.Token));
        }

        /// <summary>
        /// Stop tracking location
        /// </summary>
        public void StopTracking()
        {
            if (!_isTracking)
            {
                Console.WriteLine("LocationService: Not currently tracking");
                return;
            }

            Console.WriteLine("LocationService: Stopping tracking");
            
            _trackingCts?.Cancel();
            _trackingCts?.Dispose();
            _trackingCts = null;
            _isTracking = false;
            _lastLocation = null;
        }

        /// <summary>
        /// Main tracking loop that runs in background
        /// </summary>
        private async Task TrackingLoopAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    await UpdateLocationAsync();
                }
                catch (OperationCanceledException)
                {
                    Console.WriteLine("LocationService: Tracking cancelled");
                    break;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"LocationService: Error in tracking loop: {ex.Message}");
                }

                try
                {
                    await Task.Delay(TimeSpan.FromSeconds(UPDATE_INTERVAL_SECONDS), cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }

            Console.WriteLine("LocationService: Tracking loop ended");
        }

        /// <summary>
        /// Get current location and send to server
        /// </summary>
        private async Task UpdateLocationAsync()
        {
            try
            {
                var request = new GeolocationRequest(GeolocationAccuracy.High, TimeSpan.FromSeconds(10));
                var location = await Geolocation.GetLocationAsync(request);

                if (location == null)
                {
                    Console.WriteLine("LocationService: Could not get location");
                    return;
                }

                // Check if location has changed significantly
                if (_lastLocation != null)
                {
                    var distance = Location.CalculateDistance(_lastLocation, location, DistanceUnits.Kilometers) * 1000; // Convert to meters
                    var timeSinceLastUpdate = DateTime.Now - _lastUpdateTime;

                    // Skip update if:
                    // - Distance change is minimal AND
                    // - Less than 60 seconds since last update
                    if (distance < MIN_DISTANCE_CHANGE_METERS && timeSinceLastUpdate.TotalSeconds < 60)
                    {
                        Console.WriteLine($"LocationService: Location unchanged (moved {distance:F1}m), skipping update");
                        return;
                    }
                }

                // Calculate speed in mph
                double? speedMph = null;
                if (location.Speed.HasValue && location.Speed.Value >= 0)
                {
                    // Speed is in m/s, convert to mph
                    speedMph = location.Speed.Value * 2.23694;
                }

                // Get heading/course
                double? heading = location.Course;

                // Get accuracy in meters
                double? accuracy = location.Accuracy;

                // Send to server
                var success = await SendLocationToServerAsync(
                    location.Latitude,
                    location.Longitude,
                    speedMph,
                    heading,
                    accuracy
                );

                if (success)
                {
                    _lastLocation = location;
                    _lastUpdateTime = DateTime.Now;
                    Console.WriteLine($"LocationService: Location sent - Lat: {location.Latitude:F6}, Lng: {location.Longitude:F6}, Speed: {speedMph?.ToString("F1") ?? "N/A"} mph");
                }
            }
            catch (FeatureNotSupportedException)
            {
                Console.WriteLine("LocationService: Geolocation not supported on this device");
                StopTracking();
            }
            catch (FeatureNotEnabledException)
            {
                Console.WriteLine("LocationService: Geolocation not enabled on device");
                // Could prompt user to enable GPS
            }
            catch (PermissionException)
            {
                Console.WriteLine("LocationService: Location permission denied");
                StopTracking();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"LocationService: Error getting location: {ex.Message}");
            }
        }

        /// <summary>
        /// Send location update to the server API
        /// </summary>
        private async Task<bool> SendLocationToServerAsync(double latitude, double longitude, double? speed, double? heading, double? accuracy)
        {
            try
            {
                var authToken = Preferences.Get("AuthToken", string.Empty);
                if (string.IsNullOrEmpty(authToken))
                {
                    Console.WriteLine("LocationService: No auth token available");
                    return false;
                }

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
                    // Try to parse response
                    try
                    {
                        var result = System.Text.Json.JsonSerializer.Deserialize<LocationUpdateResponse>(responseContent);
                        if (result?.success == true)
                        {
                            return true;
                        }
                        Console.WriteLine($"LocationService: Server returned error: {result?.message}");
                    }
                    catch
                    {
                        Console.WriteLine($"LocationService: Could not parse response: {responseContent}");
                    }
                }
                else
                {
                    Console.WriteLine($"LocationService: HTTP error {response.StatusCode}: {responseContent}");
                }

                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"LocationService: Error sending location: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Check and request location permission
        /// </summary>
        private async Task<PermissionStatus> CheckAndRequestLocationPermission()
        {
            try
            {
                var status = await Permissions.CheckStatusAsync<Permissions.LocationWhenInUse>();

                if (status == PermissionStatus.Granted)
                    return status;

                if (status == PermissionStatus.Denied && DeviceInfo.Platform == DevicePlatform.iOS)
                {
                    // iOS won't ask again if denied, guide user to settings
                    Console.WriteLine("LocationService: Location permission denied on iOS - user must enable in Settings");
                    return status;
                }

                if (Permissions.ShouldShowRationale<Permissions.LocationWhenInUse>())
                {
                    // Show explanation to user (on main thread)
                    await MainThread.InvokeOnMainThreadAsync(async () =>
                    {
                        await Application.Current?.MainPage?.DisplayAlert(
                            "Location Required",
                            "This app needs your location to show your position on the map for other team members.",
                            "OK"
                        );
                    });
                }

                status = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
                Console.WriteLine($"LocationService: Permission request result: {status}");

                return status;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"LocationService: Error requesting permission: {ex.Message}");
                return PermissionStatus.Unknown;
            }
        }

        /// <summary>
        /// Force an immediate location update (useful when app comes to foreground)
        /// </summary>
        public async Task ForceUpdateAsync()
        {
            if (!_isTracking)
            {
                Console.WriteLine("LocationService: Not tracking, cannot force update");
                return;
            }

            await UpdateLocationAsync();
        }

        private class LocationUpdateResponse
        {
            public bool success { get; set; }
            public string? message { get; set; }
        }
    }
}

