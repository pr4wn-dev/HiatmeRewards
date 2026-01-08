using HiatMeApp.Models;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Storage;
using Newtonsoft.Json;
using Polly;
using Polly.Retry;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace HiatMeApp.Services
{
    public class AuthService
    {
        private readonly HttpClient _httpClient;
        private string? _csrfToken;
        private readonly AsyncRetryPolicy<(bool, string, int?)> _mileageRetryPolicy;
        private readonly AsyncRetryPolicy<(bool, User?, string)> _loginRetryPolicy;
        private readonly AsyncRetryPolicy<(bool, string)> _genericRetryPolicy;
        private readonly AsyncRetryPolicy<(bool, Vehicle?, string, List<MileageRecord>?, int?)> _assignVehicleRetryPolicy;
        private readonly AsyncRetryPolicy<(bool, List<VehicleIssue>?, string)> _vehicleIssuesRetryPolicy;
        private readonly AsyncRetryPolicy<(bool, Models.User?, string)> _updateProfileRetryPolicy;
        private readonly AsyncRetryPolicy<(bool, string)> _dayOffRequestRetryPolicy;

        public AuthService(HttpClient httpClient)
        {
            //boo
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _httpClient.Timeout = TimeSpan.FromSeconds(200);
            _csrfToken = string.Empty;
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "HiatMeApp/1.0");
            _httpClient.DefaultRequestHeaders.Add("Connection", "close"); // Prevent keep-alive issues
            Console.WriteLine("AuthService initialized with BaseAddress: " + _httpClient.BaseAddress);
            
            // Try to restore CSRF token from Preferences (if it exists and is recent)
            // But we'll still fetch a fresh one for each request to be safe
            var storedToken = Preferences.Get("CSRFToken", null);
            if (!string.IsNullOrEmpty(storedToken))
            {
                _csrfToken = storedToken;
                Console.WriteLine($"AuthService: Restored CSRF token from Preferences: {_csrfToken}");
            }

            _mileageRetryPolicy = Policy<(bool, string, int?)>
                .Handle<HttpRequestException>()
                .Or<TaskCanceledException>()
                .WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                    onRetry: (result, timeSpan, retryCount, context) =>
                    {
                        Console.WriteLine($"Mileage Retry {retryCount} after {timeSpan.TotalSeconds}s due to: {result.Exception?.Message}");
                    });

            _loginRetryPolicy = Policy<(bool, User?, string)>
                .Handle<HttpRequestException>()
                .Or<TaskCanceledException>()
                .WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                    onRetry: (result, timeSpan, retryCount, context) =>
                    {
                        Console.WriteLine($"Login Retry {retryCount} after {timeSpan.TotalSeconds}s due to: {result.Exception?.Message}");
                    });

            _genericRetryPolicy = Policy<(bool, string)>
                .Handle<HttpRequestException>()
                .Or<TaskCanceledException>()
                .WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                    onRetry: (result, timeSpan, retryCount, context) =>
                    {
                        Console.WriteLine($"Generic Retry {retryCount} after {timeSpan.TotalSeconds}s due to: {result.Exception?.Message}");
                    });

            _assignVehicleRetryPolicy = Policy<(bool, Vehicle?, string, List<MileageRecord>?, int?)>
                .Handle<HttpRequestException>()
                .Or<TaskCanceledException>()
                .WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                    onRetry: (result, timeSpan, retryCount, context) =>
                    {
                        Console.WriteLine($"AssignVehicle Retry {retryCount} after {timeSpan.TotalSeconds}s due to: {result.Exception?.Message}");
                    });

            _vehicleIssuesRetryPolicy = Policy<(bool, List<VehicleIssue>?, string)>
                .Handle<HttpRequestException>()
                .Or<TaskCanceledException>()
                .WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
        onRetry: (result, timeSpan, retryCount, context) =>
        {
            Console.WriteLine($"VehicleIssues Retry {retryCount} after {timeSpan.TotalSeconds}s due to: {result.Exception?.Message}");
        });

            _updateProfileRetryPolicy = Policy<(bool, Models.User?, string)>
                .Handle<HttpRequestException>()
                .Or<TaskCanceledException>()
                .WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                    onRetry: (result, timeSpan, retryCount, context) =>
                    {
                        Console.WriteLine($"UpdateProfile Retry {retryCount} after {timeSpan.TotalSeconds}s due to: {result.Exception?.Message}");
                    });

            _dayOffRequestRetryPolicy = Policy<(bool, string)>
                .Handle<HttpRequestException>()
                .Or<TaskCanceledException>()
                .WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                    onRetry: (result, timeSpan, retryCount, context) =>
                    {
                        Console.WriteLine($"DayOffRequest Retry {retryCount} after {timeSpan.TotalSeconds}s due to: {result.Exception?.Message}");
                    });
        }

        public async Task<bool> FetchCSRFTokenAsync()
        {
            try
            {
                string endpoint = "/includes/hiatme_config.php?action=get_csrf_token";
                Console.WriteLine($"Fetching CSRF token from: {_httpClient.BaseAddress}{endpoint}");
                LogMessage($"FetchCSRFTokenAsync: Fetching from {endpoint}");
                
                // Remove any existing CSRF token header before fetching
                _httpClient.DefaultRequestHeaders.Remove("X-CSRF-Token");
                
                var response = await _httpClient.GetAsync(endpoint);
                Console.WriteLine($"FetchCSRFTokenAsync: StatusCode={response.StatusCode}, Reason={response.ReasonPhrase}");
                LogMessage($"FetchCSRFTokenAsync: StatusCode={response.StatusCode}");

                if (!response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"FetchCSRFTokenAsync failed: Status={response.StatusCode}, Reason={response.ReasonPhrase}");
                    LogMessage($"FetchCSRFTokenAsync: Failed - StatusCode={response.StatusCode}");
                    return false;
                }

                var json = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"FetchCSRFTokenAsync response: {json}");
                LogMessage($"FetchCSRFTokenAsync: Response JSON={json}");

                if (string.IsNullOrWhiteSpace(json))
                {
                    Console.WriteLine("FetchCSRFTokenAsync: Empty response received.");
                    LogMessage("FetchCSRFTokenAsync: Empty response received");
                    return false;
                }

                var result = await Task.Run(() => JsonConvert.DeserializeObject<CsrfResponse>(json));
                if (result?.Success == true && !string.IsNullOrEmpty(result.CsrfToken))
                {
                    string oldToken = _csrfToken;
                    _csrfToken = result.CsrfToken;
                    // Store in Preferences for potential restoration (though we'll still fetch fresh)
                    Preferences.Set("CSRFToken", _csrfToken);
                    Console.WriteLine($"FetchCSRFTokenAsync: CSRF token fetched successfully: {_csrfToken} (old: {oldToken})");
                    LogMessage($"FetchCSRFTokenAsync: CSRF token fetched and stored: {_csrfToken} (old token was: {oldToken})");
                    return true;
                }
                else
                {
                    Console.WriteLine($"FetchCSRFTokenAsync: Invalid response. Success={result?.Success}, Token={result?.CsrfToken}");
                    LogMessage($"FetchCSRFTokenAsync: Invalid response - Success={result?.Success}, Token present={!string.IsNullOrEmpty(result?.CsrfToken)}");
                    return false;
                }
            }
            catch (JsonException ex)
            {
                Console.WriteLine($"FetchCSRFTokenAsync: JSON parsing error: {ex.Message}, StackTrace: {ex.StackTrace}");
                return false;
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine($"FetchCSRFTokenAsync: HTTP error: {ex.Message}, StackTrace: {ex.StackTrace}");
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"FetchCSRFTokenAsync: Unexpected error: {ex.Message}, StackTrace: {ex.StackTrace}");
                return false;
            }
        }

        /// <summary>
        /// Checks if the error message indicates that user logged in from another device/browser
        /// Only triggers if session was recently valid (within 10 minutes) to distinguish from normal expiration
        /// </summary>
        private bool IsLoggedInElsewhere(string errorMessage)
        {
            if (string.IsNullOrEmpty(errorMessage))
                return false;
            
            string lowerError = errorMessage.ToLowerInvariant();
            
            // Check for explicit server messages indicating duplicate login
            if (lowerError.Contains("logged in elsewhere") ||
                lowerError.Contains("another device") ||
                lowerError.Contains("another session") ||
                lowerError.Contains("session terminated") ||
                lowerError.Contains("new login detected") ||
                lowerError.Contains("duplicate login") ||
                lowerError.Contains("logged in on another"))
            {
                return true;
            }
            
            // For "invalid token" errors, only treat as "logged in elsewhere" if:
            // 1. User is currently logged in (IsLoggedIn = true)
            // 2. Last successful API call was recent (within 10 minutes) - indicates session was valid recently
            if (lowerError.Contains("invalid token") && Preferences.Get("IsLoggedIn", false))
            {
                string lastSuccessTimestamp = Preferences.Get("LastSuccessfulApiCall", null);
                if (!string.IsNullOrEmpty(lastSuccessTimestamp))
                {
                    if (DateTime.TryParse(lastSuccessTimestamp, out DateTime lastSuccess))
                    {
                        TimeSpan timeSinceLastSuccess = DateTime.Now - lastSuccess;
                        // Only treat as "logged in elsewhere" if last success was within 10 minutes
                        // This distinguishes from normal expiration (which would be hours/days later)
                        if (timeSinceLastSuccess.TotalMinutes <= 10)
                        {
                            LogMessage($"IsLoggedInElsewhere: Invalid token detected but last successful API call was {timeSinceLastSuccess.TotalMinutes:F1} minutes ago - likely logged in elsewhere");
                            return true;
                        }
                        else
                        {
                            LogMessage($"IsLoggedInElsewhere: Invalid token detected but last successful API call was {timeSinceLastSuccess.TotalMinutes:F1} minutes ago - likely normal expiration, not logged in elsewhere");
                            return false;
                        }
                    }
                }
                // If no timestamp stored, can't determine - err on side of caution, don't trigger
                LogMessage("IsLoggedInElsewhere: Invalid token but no last success timestamp - cannot determine if logged in elsewhere");
                return false;
            }
            
            return false;
        }
        
        /// <summary>
        /// Records the timestamp of a successful API call to track session validity
        /// </summary>
        private void RecordSuccessfulApiCall()
        {
            try
            {
                Preferences.Set("LastSuccessfulApiCall", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                LogMessage($"RecordSuccessfulApiCall: Recorded timestamp: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"RecordSuccessfulApiCall: Error recording timestamp: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Checks with the server to see if the user logged in elsewhere when we get a token error
        /// Returns true if logged in elsewhere, false if normal token error
        /// </summary>
        private async Task<bool> CheckLoggedInElsewhereWithServerAsync(string errorMessage)
        {
            try
            {
                LogMessage($"CheckLoggedInElsewhereWithServerAsync: Token error detected, validating session with server to check for logged in elsewhere. Error: {errorMessage}");
                Console.WriteLine($"CheckLoggedInElsewhereWithServerAsync: Validating session to check for logged in elsewhere");
                
                // Always check with server first before showing token error popups
                var (sessionValid, user, message) = await ValidateSessionAsync();
                
                // If session validation returns "LOGGED_IN_ELSEWHERE", handle it
                if (message.StartsWith("LOGGED_IN_ELSEWHERE:", StringComparison.OrdinalIgnoreCase))
                {
                    LogMessage($"CheckLoggedInElsewhereWithServerAsync: Server confirmed user logged in elsewhere");
                    string actualMessage = message.Substring("LOGGED_IN_ELSEWHERE:".Length);
                    _ = HandleLoggedInElsewhereAsync(actualMessage);
                    return true;
                }
                
                // If session is valid, the error was something else (not a token issue)
                if (sessionValid && user != null)
                {
                    LogMessage($"CheckLoggedInElsewhereWithServerAsync: Session is valid, error was not due to token issue");
                    return false;
                }
                
                // Session invalid but not due to logged in elsewhere - normal expiration
                LogMessage($"CheckLoggedInElsewhereWithServerAsync: Session invalid but not logged in elsewhere: {message}");
                return false;
            }
            catch (Exception ex)
            {
                LogMessage($"CheckLoggedInElsewhereWithServerAsync: Error checking with server: {ex.Message}");
                Console.WriteLine($"CheckLoggedInElsewhereWithServerAsync: Error: {ex.Message}");
                // If we can't check with server, don't treat as logged in elsewhere
                return false;
            }
        }

        /// <summary>
        /// Handles logout due to another device logging in - shows popup and navigates to login
        /// </summary>
        private async Task HandleLoggedInElsewhereAsync(string message)
        {
            try
            {
                LogMessage($"HandleLoggedInElsewhereAsync: User logged in elsewhere, clearing session and showing popup");
                Console.WriteLine($"HandleLoggedInElsewhereAsync: User logged in elsewhere, clearing session");
                
                // Clear login state
                Preferences.Set("IsLoggedIn", false);
                Preferences.Remove("AuthToken");
                Preferences.Remove("UserData");
                Preferences.Remove("CSRFToken");
                Preferences.Remove("LastSuccessfulApiCall"); // Clear timestamp
                App.CurrentUser = null;
                _csrfToken = string.Empty;
                
                // Show popup on main thread
                await MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    try
                    {
                        var page = Application.Current?.MainPage ?? Application.Current?.Windows.FirstOrDefault()?.Page;
                        if (page != null)
                        {
                            await page.DisplayAlert(
                                "Session Ended",
                                "You have been logged out because someone logged into your account from another device or browser. Please log in again to continue.",
                                "OK"
                            );
                            
                            // Navigate to login page
                            if (Shell.Current != null)
                            {
                                await Shell.Current.GoToAsync("//Login");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"HandleLoggedInElsewhereAsync: Error showing popup: {ex.Message}");
                        LogMessage($"HandleLoggedInElsewhereAsync: Error showing popup: {ex.Message}");
                    }
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"HandleLoggedInElsewhereAsync: Error: {ex.Message}");
                LogMessage($"HandleLoggedInElsewhereAsync: Error: {ex.Message}");
            }
        }

        public async Task<(bool Success, User? User, string Message)> ValidateSessionAsync()
        {
            Console.WriteLine("ValidateSessionAsync: Starting session validation");
            LogMessage("ValidateSessionAsync: Starting session validation");
            
            var authToken = Preferences.Get("AuthToken", null);
            if (string.IsNullOrEmpty(authToken))
            {
                Console.WriteLine("ValidateSessionAsync: No auth token found");
                LogMessage("ValidateSessionAsync: No auth token found");
                return (false, null, "No authentication token available.");
            }

            // Always fetch a fresh CSRF token for session validation to avoid stale token issues
            // The server regenerates tokens after each request, so we need a fresh one
            LogMessage("ValidateSessionAsync: Fetching fresh CSRF token for session validation");
            if (!await FetchCSRFTokenAsync())
            {
                Console.WriteLine("ValidateSessionAsync: Failed to retrieve CSRF token");
                LogMessage("ValidateSessionAsync: Failed to retrieve CSRF token");
                return (false, null, "Failed to retrieve session token.");
            }
            LogMessage($"ValidateSessionAsync: Fetched fresh CSRF token: {_csrfToken}");

            try
            {
                // Validate session using the validate_session endpoint
                // Include email if available to help server detect "logged in elsewhere"
                var userDataJson = Preferences.Get("UserData", string.Empty);
                string? userEmail = null;
                if (!string.IsNullOrEmpty(userDataJson))
                {
                    try
                    {
                        var storedUser = JsonConvert.DeserializeObject<Models.User>(userDataJson);
                        userEmail = storedUser?.Email;
                    }
                    catch { }
                }
                
                var data = new Dictionary<string, string>
                {
                    { "action", "validate_session" }
                };
                
                // Include email to help server detect if logged in elsewhere
                if (!string.IsNullOrEmpty(userEmail))
                {
                    data.Add("email", userEmail);
                    LogMessage($"ValidateSessionAsync: Including email in request to help detect logged in elsewhere: {userEmail}");
                }
                
                var content = new FormUrlEncodedContent(data);
                
                // Ensure Content-Type is set (FormUrlEncodedContent should do this automatically, but be explicit)
                content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/x-www-form-urlencoded");

                _httpClient.DefaultRequestHeaders.Remove("X-CSRF-Token");
                _httpClient.DefaultRequestHeaders.Add("X-CSRF-Token", _csrfToken);
                _httpClient.DefaultRequestHeaders.Remove("Authorization");
                _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {authToken}");

                Console.WriteLine($"ValidateSessionAsync: Sending validation request with auth_token={authToken}, CSRF token={_csrfToken}");
                LogMessage($"ValidateSessionAsync: Request - action=validate_session, CSRF token present={!string.IsNullOrEmpty(_csrfToken)}, Auth token present={!string.IsNullOrEmpty(authToken)}");
                
                var response = await _httpClient.PostAsync("/includes/hiatme_config.php", content);
                Console.WriteLine($"ValidateSessionAsync: StatusCode={response.StatusCode}, Reason={response.ReasonPhrase}");

                var json = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"ValidateSessionAsync response: {json}");
                LogMessage($"ValidateSessionAsync: Response StatusCode={response.StatusCode}, JSON={json}");

                if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    Console.WriteLine("ValidateSessionAsync: Session is invalid (401 Unauthorized)");
                    // Only treat as "logged in elsewhere" if session was recently valid (within 10 minutes)
                    if (Preferences.Get("IsLoggedIn", false))
                    {
                        string lastSuccessTimestamp = Preferences.Get("LastSuccessfulApiCall", null);
                        if (!string.IsNullOrEmpty(lastSuccessTimestamp) && DateTime.TryParse(lastSuccessTimestamp, out DateTime lastSuccess))
                        {
                            TimeSpan timeSinceLastSuccess = DateTime.Now - lastSuccess;
                            if (timeSinceLastSuccess.TotalMinutes <= 10)
                            {
                                LogMessage($"ValidateSessionAsync: 401 Unauthorized while logged in, last success was {timeSinceLastSuccess.TotalMinutes:F1} minutes ago - likely logged in elsewhere");
                                return (false, null, "LOGGED_IN_ELSEWHERE:Session ended. You have been logged out because someone logged into your account from another device or browser.");
                            }
                        }
                    }
                    return (false, null, "Session expired. Please log in again.");
                }
                
                // Also check for 403 Forbidden
                if (response.StatusCode == System.Net.HttpStatusCode.Forbidden)
                {
                    Console.WriteLine("ValidateSessionAsync: Session is invalid (403 Forbidden)");
                    // Only treat as "logged in elsewhere" if session was recently valid (within 10 minutes)
                    if (Preferences.Get("IsLoggedIn", false))
                    {
                        string lastSuccessTimestamp = Preferences.Get("LastSuccessfulApiCall", null);
                        if (!string.IsNullOrEmpty(lastSuccessTimestamp) && DateTime.TryParse(lastSuccessTimestamp, out DateTime lastSuccess))
                        {
                            TimeSpan timeSinceLastSuccess = DateTime.Now - lastSuccess;
                            if (timeSinceLastSuccess.TotalMinutes <= 10)
                            {
                                LogMessage($"ValidateSessionAsync: 403 Forbidden while logged in, last success was {timeSinceLastSuccess.TotalMinutes:F1} minutes ago - likely logged in elsewhere");
                                return (false, null, "LOGGED_IN_ELSEWHERE:Session ended. You have been logged out because someone logged into your account from another device or browser.");
                            }
                        }
                    }
                    return (false, null, "Session expired. Please log in again.");
                }

                // Parse as LoginResponse since validate_session returns user data in same format
                LoginResponse? result = await Task.Run(() => JsonConvert.DeserializeObject<LoginResponse>(json, new JsonSerializerSettings
                {
                    MissingMemberHandling = MissingMemberHandling.Ignore,
                    NullValueHandling = NullValueHandling.Ignore
                }));
                
                // Check if response indicates failure
                if (result == null)
                {
                    Console.WriteLine("ValidateSessionAsync: Null response from server");
                    return (false, null, "Invalid response from server.");
                }
                
                // Check for explicit failure messages that indicate invalid/expired session
                if (result.Success == false)
                {
                    string errorMsg = result.Message ?? "Session validation failed.";
                    Console.WriteLine($"ValidateSessionAsync: Session validation failed - Success=false, Message={errorMsg}");
                    LogMessage($"ValidateSessionAsync: Server response - Success=false, Message={errorMsg}, Full JSON: {json}");
                    
                    // Check if it's an invalid token/expired session
                    // "Invalid request" is NOT an expired session - it's a request format issue
                    string lowerError = errorMsg.ToLowerInvariant();
                    bool isInvalidRequest = lowerError.Contains("invalid request") && !lowerError.Contains("token");
                    
                    if (isInvalidRequest)
                    {
                        Console.WriteLine($"ValidateSessionAsync: Server returned 'Invalid request' - this is a request format issue, not expired session");
                        Console.WriteLine($"ValidateSessionAsync: Treating 'Invalid request' as network/request error, will attempt offline restore");
                        return (false, null, $"Server error: {errorMsg}. Please check your connection.");
                    }
                    
                    // Check if logged in elsewhere first
                    if (IsLoggedInElsewhere(errorMsg) || errorMsg.StartsWith("LOGGED_IN_ELSEWHERE:", StringComparison.OrdinalIgnoreCase))
                    {
                        string actualMessage = errorMsg.StartsWith("LOGGED_IN_ELSEWHERE:", StringComparison.OrdinalIgnoreCase) 
                            ? errorMsg.Substring("LOGGED_IN_ELSEWHERE:".Length) 
                            : "Session ended. You have been logged out because someone logged into your account from another device or browser.";
                        
                        Console.WriteLine($"ValidateSessionAsync: Detected logged in elsewhere: {errorMsg}");
                        LogMessage($"ValidateSessionAsync: Detected logged in elsewhere: {errorMsg}");
                        
                        // Handle the logout
                        _ = HandleLoggedInElsewhereAsync(actualMessage);
                        
                        return (false, null, $"LOGGED_IN_ELSEWHERE:{actualMessage}");
                    }
                    
                    if (lowerError.Contains("invalid token") || 
                        lowerError.Contains("expired") || 
                        lowerError.Contains("no authentication token") ||
                        lowerError.Contains("unverified") ||
                        lowerError.Contains("no token provided"))
                    {
                        // Check if logged in elsewhere - this uses timestamp logic to only trigger if session was recently valid
                        if (IsLoggedInElsewhere(errorMsg))
                        {
                            string actualMessage = "Session ended. You have been logged out because someone logged into your account from another device or browser.";
                            LogMessage($"ValidateSessionAsync: Detected logged in elsewhere via IsLoggedInElsewhere check");
                            _ = HandleLoggedInElsewhereAsync(actualMessage);
                            return (false, null, $"LOGGED_IN_ELSEWHERE:{actualMessage}");
                        }
                        
                        Console.WriteLine($"ValidateSessionAsync: Detected expired/invalid session: {errorMsg}");
                        return (false, null, "Session expired. Please log in again.");
                    }
                    
                    Console.WriteLine($"ValidateSessionAsync: Unknown error message: {errorMsg}");
                    return (false, null, errorMsg);
                }
                
                // Success must be true AND we must have valid user data
                if (result.Success == true && result.UserId > 0)
                {
                    // Record successful API call timestamp
                    RecordSuccessfulApiCall();
                    
                    // CRITICAL: Update CSRF token from server response - server regenerates it after each request
                    if (!string.IsNullOrEmpty(result.CsrfToken))
                    {
                        _csrfToken = result.CsrfToken;
                        Preferences.Set("CSRFToken", _csrfToken);
                        Console.WriteLine($"ValidateSessionAsync: Updated CSRF token from server response: {_csrfToken}");
                        LogMessage($"ValidateSessionAsync: Updated CSRF token from server response: {_csrfToken}");
                    }
                    else
                    {
                        Console.WriteLine($"ValidateSessionAsync: WARNING - No CSRF token in server response!");
                        LogMessage($"ValidateSessionAsync: WARNING - No CSRF token in server response!");
                    }
                    
                    // Session is valid, create user from server response
                    var user = new User
                    {
                        Email = result.Email,
                        Name = result.Name,
                        Phone = result.Phone,
                        ProfilePicture = result.ProfilePicture,
                        Role = result.Role,
                        UserId = result.UserId,
                        Vehicles = result.Vehicles
                    };
                    
                    // Log vehicle details including mileage records for debugging
                    if (user.Vehicles != null)
                    {
                        foreach (var vehicle in user.Vehicles)
                        {
                            if (vehicle.MileageRecord != null)
                            {
                                Console.WriteLine($"ValidateSessionAsync: Vehicle {vehicle.VehicleId} has MileageRecord - MileageId={vehicle.MileageRecord.MileageId}, StartMiles={vehicle.MileageRecord.StartMiles}, EndingMiles={vehicle.MileageRecord.EndingMiles}");
                                LogMessage($"ValidateSessionAsync: Vehicle {vehicle.VehicleId} has MileageRecord - MileageId={vehicle.MileageRecord.MileageId}, StartMiles={vehicle.MileageRecord.StartMiles}");
                            }
                            else
                            {
                                Console.WriteLine($"ValidateSessionAsync: Vehicle {vehicle.VehicleId} has NO MileageRecord");
                            }
                        }
                    }
                    
                    App.CurrentUser = user;
                    // Update stored user data with fresh data from server
                    Preferences.Set("UserData", JsonConvert.SerializeObject(user));
                    Preferences.Set("UserEmail", user.Email ?? string.Empty);
                    Preferences.Set("UserRole", user.Role ?? string.Empty);
                    Console.WriteLine($"ValidateSessionAsync: Session valid, restored user from server Email={user.Email}, Role={user.Role}, VehiclesCount={user.Vehicles?.Count ?? 0}");
                    LogMessage($"ValidateSessionAsync: Session validated successfully, Email={user.Email}, Role={user.Role}, VehiclesCount={user.Vehicles?.Count ?? 0}");
                    return (true, user, "Session is valid");
                }
                else
                {
                    Console.WriteLine($"ValidateSessionAsync: Session validation failed - Success={result.Success}, UserId={result.UserId}");
                    return (false, null, "Session validation failed - invalid response.");
                }
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine($"ValidateSessionAsync: HTTP error: {ex.Message}, InnerException: {ex.InnerException?.Message ?? "none"}");
                // Check for 401/403 status codes
                if (ex.Message.Contains("401") || 
                    ex.Message.Contains("Unauthorized") ||
                    ex.Message.Contains("403") ||
                    ex.Message.Contains("Forbidden") ||
                    (ex.InnerException != null && (ex.InnerException.Message.Contains("401") || ex.InnerException.Message.Contains("403"))))
                {
                    Console.WriteLine($"ValidateSessionAsync: Detected 401/403 - session expired");
                    return (false, null, "Session expired. Please log in again.");
                }
                Console.WriteLine($"ValidateSessionAsync: Network error (not 401/403): {ex.Message}");
                return (false, null, $"Network error: {ex.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ValidateSessionAsync: Unexpected error: {ex.Message}");
                return (false, null, $"An error occurred: {ex.Message}");
            }
        }

        public async Task<(bool Success, User? User, string Message)> LoginAsync(string? email, string? password)
        {
            LogMessage($"LoginAsync: Starting login for Email={email}");
            
            if (string.IsNullOrEmpty(_csrfToken))
            {
                LogMessage("LoginAsync: No CSRF token, fetching new one");
                if (!await FetchCSRFTokenAsync())
                {
                    LogMessage("LoginAsync: Failed to fetch CSRF token");
                    return (false, null, "Failed to retrieve session token.");
                }
            }
            
            LogMessage($"LoginAsync: Using CSRF token={_csrfToken}");

            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
                return (false, null, "Email and password are required.");

            var data = new Dictionary<string, string>
            {
                { "action", "login" },
                { "email", email },
                { "password", password }
            };
            var content = new FormUrlEncodedContent(data);
            _httpClient.DefaultRequestHeaders.Remove("X-CSRF-Token");
            _httpClient.DefaultRequestHeaders.Add("X-CSRF-Token", _csrfToken);

            try
            {
                return await _loginRetryPolicy.ExecuteAsync(async () =>
                {
                    Console.WriteLine($"LoginAsync: Sending request for Email={email}, CSRF={_csrfToken}");
                    LogMessage($"LoginAsync: Sending login request with CSRF token={_csrfToken}");
                    var response = await _httpClient.PostAsync("/includes/hiatme_config.php", content);
                    Console.WriteLine($"LoginAsync: StatusCode={response.StatusCode}, Reason={response.ReasonPhrase}");
                    Console.WriteLine("LoginAsync: Response headers:");
                    foreach (var header in response.Headers)
                    {
                        Console.WriteLine($"LoginAsync: {header.Key}: {string.Join(", ", header.Value)}");
                    }

                    var json = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"LoginAsync raw response: {json}");
                    LogMessage($"LoginAsync: Response StatusCode={response.StatusCode}, JSON={json}");

                    if (string.IsNullOrWhiteSpace(json))
                    {
                        Console.WriteLine("LoginAsync: Empty response received.");
                        if (await FetchCSRFTokenAsync())
                            return (false, null, "Empty response. Please try again with new session token.");
                        return (false, null, "Empty response and unable to refresh session token.");
                    }

                    LoginResponse? result = await Task.Run(() => JsonConvert.DeserializeObject<LoginResponse>(json, new JsonSerializerSettings
                    {
                        MissingMemberHandling = MissingMemberHandling.Ignore,
                        NullValueHandling = NullValueHandling.Ignore
                    }));

                    if (!string.IsNullOrEmpty(result?.CsrfToken))
                    {
                        _csrfToken = result.CsrfToken;
                        Preferences.Set("CSRFToken", _csrfToken);
                        Console.WriteLine($"LoginAsync: Updated CSRF token: {_csrfToken}");
                        LogMessage($"LoginAsync: Updated CSRF token: {_csrfToken}");
                    }
                    else
                    {
                        Console.WriteLine("LoginAsync: No CSRF token in response. Fetching new token.");
                        if (!await FetchCSRFTokenAsync())
                        {
                            Console.WriteLine("LoginAsync: Failed to fetch new CSRF token.");
                            return (false, null, "Unable to refresh session token.");
                        }
                        Console.WriteLine($"LoginAsync: Fetched new CSRF token: {_csrfToken}");
                    }

                    if (result?.Success == true)
                    {
                        var user = new User
                        {
                            Email = result.Email,
                            Name = result.Name,
                            Phone = result.Phone,
                            ProfilePicture = result.ProfilePicture,
                            Role = result.Role,
                            UserId = result.UserId,
                            Vehicles = result.Vehicles
                        };
                        App.CurrentUser = user;
                        Preferences.Set("UserEmail", user.Email);
                        Preferences.Set("IsLoggedIn", true);
                        Preferences.Set("UserData", JsonConvert.SerializeObject(user));
                        Preferences.Set("AuthToken", result.AuthToken);
                        // Record successful API call timestamp on login
                        RecordSuccessfulApiCall();
                        
                        Console.WriteLine($"LoginAsync: Login successful for Email={email}, Role={result.Role}, UserId={result.UserId}, VehiclesCount={result.Vehicles?.Count ?? 0}, AuthToken={result.AuthToken}");
                        Console.WriteLine($"LoginAsync: ProfilePicture from server: '{result.ProfilePicture ?? "null"}' (length: {result.ProfilePicture?.Length ?? 0})");
                        Console.WriteLine($"LoginAsync: User.ProfilePicture after assignment: '{user.ProfilePicture ?? "null"}' (length: {user.ProfilePicture?.Length ?? 0})");
                        return (true, user, "Login successful");
                    }
                    else
                    {
                        string errorMsg = result?.Message ?? "Unknown error";
                        Console.WriteLine($"LoginAsync failed: {errorMsg}");
                        LogMessage($"LoginAsync: Login failed - Message={errorMsg}");
                        
                        // If CSRF token error, try fetching a new token and retrying once
                        if (errorMsg.Contains("CSRF token") || errorMsg.Contains("Invalid CSRF") || errorMsg.Contains("csrf") || errorMsg.Contains("session token"))
                        {
                            LogMessage("LoginAsync: CSRF token error detected, fetching new token and retrying");
                            if (await FetchCSRFTokenAsync())
                            {
                                LogMessage($"LoginAsync: Fetched new CSRF token={_csrfToken}, retrying login");
                                // Retry with new token
                                _httpClient.DefaultRequestHeaders.Remove("X-CSRF-Token");
                                _httpClient.DefaultRequestHeaders.Add("X-CSRF-Token", _csrfToken);
                                
                                var retryResponse = await _httpClient.PostAsync("/includes/hiatme_config.php", content);
                                var retryJson = await retryResponse.Content.ReadAsStringAsync();
                                LogMessage($"LoginAsync: Retry response StatusCode={retryResponse.StatusCode}, JSON={retryJson}");
                                
                                var retryResult = await Task.Run(() => JsonConvert.DeserializeObject<LoginResponse>(retryJson, new JsonSerializerSettings
                                {
                                    MissingMemberHandling = MissingMemberHandling.Ignore,
                                    NullValueHandling = NullValueHandling.Ignore
                                }));
                                
                                if (retryResult?.Success == true)
                                {
                                    // Handle successful retry
                                    if (!string.IsNullOrEmpty(retryResult.CsrfToken))
                                    {
                                        _csrfToken = retryResult.CsrfToken;
                                    }
                                    var user = new User
                                    {
                                        Email = retryResult.Email,
                                        Name = retryResult.Name,
                                        Phone = retryResult.Phone,
                                        ProfilePicture = retryResult.ProfilePicture,
                                        Role = retryResult.Role,
                                        UserId = retryResult.UserId,
                                        Vehicles = retryResult.Vehicles
                                    };
                                    App.CurrentUser = user;
                                    Preferences.Set("UserEmail", user.Email);
                                    Preferences.Set("IsLoggedIn", true);
                                    Preferences.Set("UserData", JsonConvert.SerializeObject(user));
                                    Preferences.Set("AuthToken", retryResult.AuthToken);
                                    LogMessage($"LoginAsync: Retry successful for Email={email}");
                                    return (true, user, "Login successful");
                                }
                                else
                                {
                                    LogMessage($"LoginAsync: Retry also failed - {retryResult?.Message ?? "Unknown error"}");
                                }
                            }
                        }
                        
                        return (false, null, errorMsg);
                    }
                });
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine($"LoginAsync: HTTP error: {ex.Message}, StackTrace: {ex.StackTrace}");
                if (await FetchCSRFTokenAsync())
                    return (false, null, "Network error. Please check your connection and try again.");
                return (false, null, "Network error and unable to refresh session token.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"LoginAsync: Unexpected error: {ex.Message}, StackTrace: {ex.StackTrace}");
                if (await FetchCSRFTokenAsync())
                    return (false, null, "An error occurred. Please check your connection and try again.");
                return (false, null, "An error occurred and unable to refresh session token.");
            }
        }

        public async Task<(bool Success, string Message)> RegisterAsync(string? name, string? email, string? phone, string? password)
        {
            Console.WriteLine($"RegisterAsync: Starting for Email={email}");
            if (string.IsNullOrEmpty(_csrfToken) && !await FetchCSRFTokenAsync())
            {
                Console.WriteLine("RegisterAsync: Failed to retrieve CSRF token.");
                return (false, "Failed to retrieve session token.");
            }

            if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
            {
                Console.WriteLine("RegisterAsync: Missing required fields.");
                return (false, "Name, email, and password are required.");
            }

            var data = new Dictionary<string, string>
            {
                { "action", "register" },
                { "name", name },
                { "email", email },
                { "phone", phone ?? string.Empty },
                { "password", password }
            };
            var content = new FormUrlEncodedContent(data);
            _httpClient.DefaultRequestHeaders.Remove("X-CSRF-Token");
            _httpClient.DefaultRequestHeaders.Add("X-CSRF-Token", _csrfToken);

            try
            {
                return await _genericRetryPolicy.ExecuteAsync(async () =>
                {
                    Console.WriteLine($"RegisterAsync: Sending POST request with CSRF={_csrfToken}, Data={JsonConvert.SerializeObject(data)}");
                    var response = await _httpClient.PostAsync("/includes/hiatme_config.php", content);
                    Console.WriteLine($"RegisterAsync: StatusCode={response.StatusCode}, Reason={response.ReasonPhrase}");
                    Console.WriteLine("RegisterAsync: Response headers:");
                    foreach (var header in response.Headers)
                    {
                        Console.WriteLine($"RegisterAsync: {header.Key}: {string.Join(", ", header.Value)}");
                    }

                    var json = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"RegisterAsync response: {json}");

                    if (string.IsNullOrWhiteSpace(json))
                    {
                        Console.WriteLine("RegisterAsync: Empty response received.");
                        return (false, "Empty response from server.");
                    }

                    GenericResponse? result = await Task.Run(() => JsonConvert.DeserializeObject<GenericResponse>(json));
                    if (result == null)
                    {
                        Console.WriteLine("RegisterAsync: Deserialized response is null.");
                        return (false, "Invalid response format from server.");
                    }

                    if (!string.IsNullOrEmpty(result.CsrfToken))
                    {
                        _csrfToken = result.CsrfToken;
                        Preferences.Set("CSRFToken", _csrfToken);
                        Console.WriteLine($"RegisterAsync: Updated CSRF token: {_csrfToken}");
                        LogMessage($"RegisterAsync: Updated CSRF token: {_csrfToken}");
                    }

                    if (result.Success)
                    {
                        Console.WriteLine($"RegisterAsync: Registration successful for Email={email}");
                        return (true, result.Message ?? "Registration successful.");
                    }
                    else
                    {
                        string errorMsg = result.Message ?? "Unknown error";
                        Console.WriteLine($"RegisterAsync failed: {errorMsg}");
                        
                        // If CSRF token error, try fetching a new token and retrying once
                        if (errorMsg.Contains("CSRF token") || errorMsg.Contains("Invalid CSRF") || errorMsg.Contains("csrf") || errorMsg.Contains("session token"))
                        {
                            LogMessage($"RegisterAsync: CSRF token error detected: {errorMsg}, fetching new token and retrying");
                            if (await FetchCSRFTokenAsync())
                            {
                                _httpClient.DefaultRequestHeaders.Remove("X-CSRF-Token");
                                _httpClient.DefaultRequestHeaders.Add("X-CSRF-Token", _csrfToken);
                                var retryResponse = await _httpClient.PostAsync("/includes/hiatme_config.php", content);
                                var retryJson = await retryResponse.Content.ReadAsStringAsync();
                                var retryResult = await Task.Run(() => JsonConvert.DeserializeObject<GenericResponse>(retryJson));
                                
                                if (retryResult?.Success == true)
                                {
                                    if (!string.IsNullOrEmpty(retryResult.CsrfToken))
                                    {
                                        _csrfToken = retryResult.CsrfToken;
                                        Preferences.Set("CSRFToken", _csrfToken);
                                    }
                                    LogMessage($"RegisterAsync: Retry successful");
                                    return (true, retryResult.Message ?? "Registration successful.");
                                }
                            }
                        }
                        
                        return (false, errorMsg);
                    }
                });
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine($"RegisterAsync: HTTP error: {ex.Message}, StackTrace: {ex.StackTrace}");
                return (false, $"Network error: {ex.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"RegisterAsync: Unexpected error: {ex.Message}, StackTrace: {ex.StackTrace}");
                return (false, $"An error occurred: {ex.Message}");
            }
        }

        public async Task<(bool Success, string Message)> ForgotPasswordAsync(string? email)
        {
            Console.WriteLine($"ForgotPasswordAsync: Starting for Email={email}");
            if (string.IsNullOrEmpty(_csrfToken) && !await FetchCSRFTokenAsync())
            {
                Console.WriteLine("ForgotPasswordAsync: Failed to retrieve CSRF token.");
                return (false, "Failed to retrieve session token.");
            }

            if (string.IsNullOrEmpty(email))
            {
                Console.WriteLine("ForgotPasswordAsync: Missing email.");
                return (false, "Email is required.");
            }

            var data = new Dictionary<string, string>
            {
                { "action", "forgot_password" },
                { "email", email }
            };
            var content = new FormUrlEncodedContent(data);
            _httpClient.DefaultRequestHeaders.Remove("X-CSRF-Token");
            _httpClient.DefaultRequestHeaders.Add("X-CSRF-Token", _csrfToken);

            try
            {
                return await _genericRetryPolicy.ExecuteAsync(async () =>
                {
                    Console.WriteLine($"ForgotPasswordAsync: Sending POST request with CSRF={_csrfToken}");
                    var response = await _httpClient.PostAsync("/includes/hiatme_config.php", content);
                    Console.WriteLine($"ForgotPasswordAsync: StatusCode={response.StatusCode}, Reason={response.ReasonPhrase}");
                    Console.WriteLine("ForgotPasswordAsync: Response headers:");
                    foreach (var header in response.Headers)
                    {
                        Console.WriteLine($"ForgotPasswordAsync: {header.Key}: {string.Join(", ", header.Value)}");
                    }

                    var json = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"ForgotPasswordAsync response: {json}");

                    GenericResponse? result = await Task.Run(() => JsonConvert.DeserializeObject<GenericResponse>(json));
                    if (result == null)
                    {
                        Console.WriteLine("ForgotPasswordAsync: Deserialized response is null.");
                        return (false, "Invalid response format from server.");
                    }

                    if (!string.IsNullOrEmpty(result.CsrfToken))
                    {
                        _csrfToken = result.CsrfToken;
                        Preferences.Set("CSRFToken", _csrfToken);
                        Console.WriteLine($"ForgotPasswordAsync: Updated CSRF token: {_csrfToken}");
                        LogMessage($"ForgotPasswordAsync: Updated CSRF token: {_csrfToken}");
                    }

                    if (result.Success)
                    {
                        Console.WriteLine($"ForgotPasswordAsync: Request successful for Email={email}");
                        return (true, result.Message ?? "Reset link sent.");
                    }
                    else
                    {
                        Console.WriteLine($"ForgotPasswordAsync failed: {result.Message ?? "Unknown error"}");
                        return (false, result.Message ?? "Failed to send reset link.");
                    }
                });
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine($"ForgotPasswordAsync: HTTP error: {ex.Message}, StackTrace: {ex.StackTrace}");
                return (false, $"Network error: {ex.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ForgotPasswordAsync: Unexpected error: {ex.Message}, StackTrace: {ex.StackTrace}");
                return (false, $"An error occurred: {ex.Message}");
            }
        }

        public async Task<(bool Success, Vehicle? Vehicle, string Message, List<MileageRecord>? IncompleteRecords, int? MileageId)> AssignVehicleAsync(string? vinSuffix, bool allowIncompleteEndingMiles = false)
        {
            Console.WriteLine($"AssignVehicleAsync: Starting with VIN suffix={vinSuffix}, allowIncompleteEndingMiles={allowIncompleteEndingMiles}");
            if (string.IsNullOrEmpty(_csrfToken) && !await FetchCSRFTokenAsync())
            {
                Console.WriteLine("AssignVehicleAsync: Failed to retrieve CSRF token.");
                return (false, null, "Failed to retrieve session token.", null, null);
            }

            if (string.IsNullOrEmpty(vinSuffix) || vinSuffix.Length != 6 || !System.Text.RegularExpressions.Regex.IsMatch(vinSuffix, @"^[A-HJ-NPR-Z0-9]{6}$", System.Text.RegularExpressions.RegexOptions.IgnoreCase))
            {
                Console.WriteLine("AssignVehicleAsync: Invalid VIN suffix.");
                return (false, null, "VIN suffix must be exactly 6 alphanumeric characters (excluding I, O, Q).", null, null);
            }

            var authToken = Preferences.Get("AuthToken", null);
            if (string.IsNullOrEmpty(authToken))
            {
                Console.WriteLine("AssignVehicleAsync: No auth_token found in Preferences.");
                return (false, null, "No authentication token available. Please log in again.", null, null);
            }

            var data = new Dictionary<string, string>
            {
                { "action", allowIncompleteEndingMiles ? "assign_vehicle_by_vin_allow_incomplete" : "assign_vehicle_by_vin" },
                { "vin_suffix", vinSuffix }
            };
            var content = new FormUrlEncodedContent(data);

            _httpClient.DefaultRequestHeaders.Remove("X-CSRF-Token");
            _httpClient.DefaultRequestHeaders.Add("X-CSRF-Token", _csrfToken);
            _httpClient.DefaultRequestHeaders.Remove("Authorization");
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {authToken}");

            try
            {
                return await _assignVehicleRetryPolicy.ExecuteAsync(async () =>
                {
                    Console.WriteLine($"AssignVehicleAsync: Sending POST request with CSRF={_csrfToken}, VIN suffix={vinSuffix}, auth_token={authToken}, action={data["action"]}");
                    var response = await _httpClient.PostAsync("/includes/hiatme_config.php", content);
                    Console.WriteLine($"AssignVehicleAsync: StatusCode={response.StatusCode}, Reason={response.ReasonPhrase}");
                    Console.WriteLine("AssignVehicleAsync: Response headers:");
                    foreach (var header in response.Headers)
                    {
                        Console.WriteLine($"AssignVehicleAsync: {header.Key}: {string.Join(", ", header.Value)}");
                    }

                    var json = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"AssignVehicleAsync raw response: {json}");

                    if (string.IsNullOrWhiteSpace(json))
                    {
                        Console.WriteLine("AssignVehicleAsync: Empty response received.");
                        return (false, null, "Empty response from server.", null, null);
                    }

                    AssignVehicleResponse? result = await Task.Run(() => JsonConvert.DeserializeObject<AssignVehicleResponse>(json));
                    if (result == null)
                    {
                        Console.WriteLine("AssignVehicleAsync: Deserialized response is null.");
                        return (false, null, "Invalid response format from server.", null, null);
                    }

                    if (!string.IsNullOrEmpty(result.CsrfToken))
                    {
                        _csrfToken = result.CsrfToken;
                        Preferences.Set("CSRFToken", _csrfToken);
                        Console.WriteLine($"AssignVehicleAsync: Updated CSRF token: {_csrfToken}");
                        LogMessage($"AssignVehicleAsync: Updated CSRF token: {_csrfToken}");
                    }

                    if (result.Success && result.Vehicle != null)
                    {
                        Console.WriteLine($"AssignVehicleAsync: Vehicle assigned successfully, VIN={result.Vehicle.Vin}, VehicleId={result.Vehicle.VehicleId}, CurrentUserId={result.Vehicle.CurrentUserId}, DateAssigned={result.Vehicle.DateAssigned}, MileageId={result.MileageId}");
                        return (true, result.Vehicle, result.Message ?? "Vehicle assigned successfully.", null, result.MileageId);
                    }
                    else
                    {
                        Console.WriteLine($"AssignVehicleAsync failed: {result.Message ?? "Unknown error"}");
                        if (result.Message == "Invalid request" && await FetchCSRFTokenAsync())
                        {
                            Console.WriteLine("AssignVehicleAsync: Retrying with new CSRF token");
                            _httpClient.DefaultRequestHeaders.Remove("X-CSRF-Token");
                            _httpClient.DefaultRequestHeaders.Add("X-CSRF-Token", _csrfToken);
                            var retryResponse = await _httpClient.PostAsync("/includes/hiatme_config.php", content);
                            var retryJson = await retryResponse.Content.ReadAsStringAsync();
                            Console.WriteLine($"AssignVehicleAsync retry response: {retryJson}");

                            AssignVehicleResponse? retryResult = await Task.Run(() => JsonConvert.DeserializeObject<AssignVehicleResponse>(retryJson));
                            if (retryResult?.Success == true && retryResult.Vehicle != null)
                            {
                                Console.WriteLine($"AssignVehicleAsync retry: Vehicle assigned successfully, VIN={retryResult.Vehicle.Vin}, VehicleId={retryResult.Vehicle.VehicleId}, CurrentUserId={retryResult.Vehicle.CurrentUserId}, DateAssigned={retryResult.Vehicle.DateAssigned}, MileageId={retryResult.MileageId}");
                                if (!string.IsNullOrEmpty(retryResult.CsrfToken))
                                {
                                    _csrfToken = retryResult.CsrfToken;
                                    Console.WriteLine($"AssignVehicleAsync retry: Updated CSRF token: {_csrfToken}");
                                }
                                return (true, retryResult.Vehicle, retryResult.Message ?? "Vehicle assigned successfully.", null, retryResult.MileageId);
                            }
                            else
                            {
                                Console.WriteLine($"AssignVehicleAsync retry failed: {retryResult?.Message ?? "Unknown error"}");
                                return (false, null, retryResult?.Message ?? "Failed to assign vehicle.", retryResult?.IncompleteRecords, null);
                            }
                        }
                        return (false, null, result.Message ?? "Failed to assign vehicle.", result.IncompleteRecords, null);
                    }
                });
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine($"AssignVehicleAsync: HTTP error: {ex.Message}, StackTrace: {ex.StackTrace}");
                return (false, null, $"Network error: {ex.Message}", null, null);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"AssignVehicleAsync: Unexpected error: {ex.Message}, StackTrace: {ex.StackTrace}");
                return (false, null, $"An error occurred: {ex.Message}", null, null);
            }
        }

        public async Task<(bool Success, string Message, int? MileageId)> SubmitStartMileageAsync(int mileageId, double startMiles)
        {
            Console.WriteLine($"SubmitStartMileageAsync: Starting with mileage_id={mileageId}, start_miles={startMiles}");
            LogMessage($"SubmitStartMileageAsync: Starting with mileage_id={mileageId}, start_miles={startMiles}");
            
            // CRITICAL: Use the token from the last successful response (stored in Preferences)
            // The server regenerates tokens after each request, so we MUST use the token from the PREVIOUS request
            // DO NOT fetch a fresh token - that will cause "Invalid CSRF token" errors
            var storedToken = Preferences.Get("CSRFToken", null);
            if (!string.IsNullOrEmpty(storedToken))
            {
                _csrfToken = storedToken;
                LogMessage($"SubmitStartMileageAsync: Using CSRF token from last response (Preferences): {_csrfToken}");
            }
            else if (string.IsNullOrEmpty(_csrfToken))
            {
                // Only fetch if we have NO token at all (shouldn't happen after validate_session)
                LogMessage("SubmitStartMileageAsync: WARNING - No token found, fetching fresh CSRF token");
                if (!await FetchCSRFTokenAsync())
                {
                    LogMessage("SubmitStartMileageAsync: Failed to fetch CSRF token");
                    return (false, "Failed to retrieve session token.", null);
                }
                LogMessage($"SubmitStartMileageAsync: Fetched fresh CSRF token: {_csrfToken}");
            }
            else
            {
                LogMessage($"SubmitStartMileageAsync: Using existing CSRF token from memory: {_csrfToken}");
            }

            var authToken = Preferences.Get("AuthToken", null);
            if (string.IsNullOrEmpty(authToken))
                return (false, "No authentication token available. Please log in again.", null);

            var startMilesDatetime = TimeZoneInfo.ConvertTime(DateTime.Now, TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time")).ToString("yyyy-MM-dd HH:mm:ss");
            var data = new Dictionary<string, string>
            {
                { "action", "submit_start_mileage" },
                { "mileage_id", mileageId.ToString() },
                { "start_miles", startMiles.ToString(System.Globalization.CultureInfo.InvariantCulture) },
                { "start_miles_datetime", startMilesDatetime }
            };
            var content = new FormUrlEncodedContent(data);

            _httpClient.DefaultRequestHeaders.Remove("X-CSRF-Token");
            _httpClient.DefaultRequestHeaders.Add("X-CSRF-Token", _csrfToken);
            _httpClient.DefaultRequestHeaders.Remove("Authorization");
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {authToken}");

            try
            {
                return await _mileageRetryPolicy.ExecuteAsync(async () =>
                {
                    Console.WriteLine($"SubmitStartMileageAsync: Sending POST request with CSRF={_csrfToken}, mileage_id={mileageId}, start_miles={startMiles}, start_miles_datetime={startMilesDatetime}");
                    LogMessage($"SubmitStartMileageAsync: Sending request with CSRF token={_csrfToken}, mileage_id={mileageId}, start_miles={startMiles}");
                    var response = await _httpClient.PostAsync("/includes/hiatme_config.php", content);
                    Console.WriteLine($"SubmitStartMileageAsync: StatusCode={response.StatusCode}, Reason={response.ReasonPhrase}");
                    LogMessage($"SubmitStartMileageAsync: Response StatusCode={response.StatusCode}");
                    Console.WriteLine("SubmitStartMileageAsync: Response headers:");
                    foreach (var header in response.Headers)
                    {
                        Console.WriteLine($"SubmitStartMileageAsync: {header.Key}: {string.Join(", ", header.Value)}");
                    }

                    var json = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"SubmitStartMileageAsync response: {json}");

                    if (string.IsNullOrWhiteSpace(json))
                        return (false, "Empty response from server.", null);

                    SubmitMileageResponse? result = await Task.Run(() => JsonConvert.DeserializeObject<SubmitMileageResponse>(json));
                    if (result == null)
                        return (false, "Invalid response format from server.", null);

                    if (!string.IsNullOrEmpty(result.CsrfToken))
                    {
                        _csrfToken = result.CsrfToken;
                        Preferences.Set("CSRFToken", _csrfToken);
                    }

                    Console.WriteLine($"SubmitStartMileageAsync: Updated CSRF token: {_csrfToken}");
                    LogMessage($"SubmitStartMileageAsync: Response Success={result.Success}, Message={result.Message}");
                    
                    if (result.Success)
                    {
                        Console.WriteLine($"SubmitStartMileageAsync: Successfully submitted start mileage for vehicle_id={result.VehicleId}, mileage_id={result.MileageId}");
                        return (true, result.Message ?? "Start mileage submitted successfully.", result.MileageId);
                    }
                    else
                    {
                        string errorMsg = result.Message ?? "Failed to submit start mileage.";
                        Console.WriteLine($"SubmitStartMileageAsync failed: {errorMsg}");
                        LogMessage($"SubmitStartMileageAsync: Error - {errorMsg}");
                        
                        // If CSRF token error, try fetching a new token and retrying once
                        if (errorMsg.Contains("CSRF token") || errorMsg.Contains("Invalid CSRF") || errorMsg.Contains("csrf") || errorMsg.Contains("session token"))
                        {
                            LogMessage($"SubmitStartMileageAsync: CSRF token error detected, fetching new token and retrying");
                            if (await FetchCSRFTokenAsync())
                            {
                                _httpClient.DefaultRequestHeaders.Remove("X-CSRF-Token");
                                _httpClient.DefaultRequestHeaders.Add("X-CSRF-Token", _csrfToken);
                                LogMessage($"SubmitStartMileageAsync: Retrying with fresh CSRF token={_csrfToken}");
                                
                                var retryResponse = await _httpClient.PostAsync("/includes/hiatme_config.php", content);
                                var retryJson = await retryResponse.Content.ReadAsStringAsync();
                                LogMessage($"SubmitStartMileageAsync: Retry response StatusCode={retryResponse.StatusCode}, JSON={retryJson}");
                                
                                var retryResult = await Task.Run(() => JsonConvert.DeserializeObject<SubmitMileageResponse>(retryJson));
                                
                                if (retryResult?.Success == true)
                                {
                                    if (!string.IsNullOrEmpty(retryResult.CsrfToken))
                                    {
                                        _csrfToken = retryResult.CsrfToken;
                                        Preferences.Set("CSRFToken", _csrfToken);
                                    }
                                    LogMessage($"SubmitStartMileageAsync: Retry successful");
                                    return (true, retryResult.Message ?? "Start mileage submitted successfully.", retryResult.MileageId);
                                }
                                else
                                {
                                    LogMessage($"SubmitStartMileageAsync: Retry also failed - {retryResult?.Message ?? "Unknown error"}");
                                }
                            }
                        }
                        
                        return (false, errorMsg, null);
                    }
                });
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine($"SubmitStartMileageAsync: HTTP error: {ex.Message}, StackTrace: {ex.StackTrace}");
                return (false, $"Network error: {ex.Message}", null);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"SubmitStartMileageAsync: Unexpected error: {ex.Message}, StackTrace: {ex.StackTrace}");
                return (false, $"An error occurred: {ex.Message}", null);
            }
        }

        public async Task<(bool Success, string Message, int? MileageId)> SubmitEndMileageAsync(int vehicleId, double endingMiles)
        {
            Console.WriteLine($"SubmitEndMileageAsync: Starting with vehicle_id={vehicleId}, ending_miles={endingMiles}");
            if (string.IsNullOrEmpty(_csrfToken) && !await FetchCSRFTokenAsync())
            {
                Console.WriteLine("SubmitEndMileageAsync: Failed to retrieve CSRF token.");
                return (false, "Failed to retrieve session token.", null);
            }

            var authToken = Preferences.Get("AuthToken", null);
            if (string.IsNullOrEmpty(authToken))
            {
                Console.WriteLine("SubmitEndMileageAsync: No auth_token found in Preferences.");
                return (false, "No authentication token available. Please log in again.", null);
            }

            var endingMilesDatetime = TimeZoneInfo.ConvertTime(DateTime.Now, TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time")).ToString("yyyy-MM-dd HH:mm:ss");
            var data = new Dictionary<string, string>
            {
                { "action", "submit_end_mileage" },
                { "vehicle_id", vehicleId.ToString() },
                { "ending_miles", endingMiles.ToString(System.Globalization.CultureInfo.InvariantCulture) },
                { "ending_miles_datetime", endingMilesDatetime }
            };
            var content = new FormUrlEncodedContent(data);

            _httpClient.DefaultRequestHeaders.Remove("X-CSRF-Token");
            _httpClient.DefaultRequestHeaders.Add("X-CSRF-Token", _csrfToken);
            _httpClient.DefaultRequestHeaders.Remove("Authorization");
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {authToken}");

            try
            {
                return await _mileageRetryPolicy.ExecuteAsync(async () =>
                {
                    Console.WriteLine($"SubmitEndMileageAsync: Sending POST request with CSRF={_csrfToken}, vehicle_id={vehicleId}, ending_miles={endingMiles}, ending_miles_datetime={endingMilesDatetime}");
                    var response = await _httpClient.PostAsync("/includes/hiatme_config.php", content);
                    Console.WriteLine($"SubmitEndMileageAsync: StatusCode={response.StatusCode}, Reason={response.ReasonPhrase}");
                    Console.WriteLine("SubmitEndMileageAsync: Response headers:");
                    foreach (var header in response.Headers)
                    {
                        Console.WriteLine($"SubmitEndMileageAsync: {header.Key}: {string.Join(", ", header.Value)}");
                    }

                    var json = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"SubmitEndMileageAsync response: {json}");

                    if (string.IsNullOrWhiteSpace(json))
                    {
                        Console.WriteLine("SubmitEndMileageAsync: Empty response received.");
                        return (false, "Empty response from server.", null);
                    }

                    SubmitMileageResponse? result = await Task.Run(() => JsonConvert.DeserializeObject<SubmitMileageResponse>(json));
                    if (result == null)
                    {
                        Console.WriteLine("SubmitEndMileageAsync: Deserialized response is null.");
                        return (false, "Invalid response format from server.", null);
                    }

                    if (!string.IsNullOrEmpty(result.CsrfToken))
                    {
                        _csrfToken = result.CsrfToken;
                        Preferences.Set("CSRFToken", _csrfToken);
                        Console.WriteLine($"SubmitEndMileageAsync: Updated CSRF token: {_csrfToken}");
                        LogMessage($"SubmitEndMileageAsync: Updated CSRF token: {_csrfToken}");
                    }

                    Console.WriteLine($"SubmitEndMileageAsync: Successfully submitted end mileage for vehicle_id={result.VehicleId}, mileage_id={result.MileageId}");
                    return (result.Success, result.Message ?? "Failed to submit end mileage.", result.MileageId);
                });
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine($"SubmitEndMileageAsync: HTTP error: {ex.Message}, StackTrace: {ex.StackTrace}");
                return (false, $"Network error: {ex.Message}", null);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"SubmitEndMileageAsync: Unexpected error: {ex.Message}, StackTrace: {ex.StackTrace}");
                return (false, $"An error occurred: {ex.Message}", null);
            }
        }

        public async Task<(bool Success, List<VehicleIssue>? Issues, string Message)> GetVehicleIssuesAsync(int vehicleId)
        {
            Console.WriteLine($"GetVehicleIssuesAsync: Starting for vehicle_id={vehicleId}");
            LogMessage($"GetVehicleIssuesAsync: Starting for vehicle_id={vehicleId}");
            
            // Use stored token from Preferences (from last successful response)
            var storedToken = Preferences.Get("CSRFToken", null);
            if (!string.IsNullOrEmpty(storedToken))
            {
                _csrfToken = storedToken;
                LogMessage($"GetVehicleIssuesAsync: Using CSRF token from last response (Preferences): {_csrfToken}");
            }
            else if (string.IsNullOrEmpty(_csrfToken))
            {
                // Only fetch if we have NO token at all
                LogMessage("GetVehicleIssuesAsync: No token found, fetching fresh CSRF token");
                if (!await FetchCSRFTokenAsync())
                {
                    Console.WriteLine("GetVehicleIssuesAsync: Failed to retrieve CSRF token.");
                    LogMessage("GetVehicleIssuesAsync: Failed to retrieve CSRF token.");
                    return (false, null, "Failed to retrieve session token.");
                }
            }

            var authToken = Preferences.Get("AuthToken", null);
            if (string.IsNullOrEmpty(authToken))
            {
                Console.WriteLine("GetVehicleIssuesAsync: No auth_token found in Preferences.");
                LogMessage("GetVehicleIssuesAsync: No auth_token found in Preferences.");
                return (false, null, "No authentication token available. Please log in again.");
            }

            var data = new Dictionary<string, string>
        {
            { "action", "get_vehicle_issues" },
            { "vehicle_id", vehicleId.ToString() },
            { "auth_token", authToken }
        };
            var content = new FormUrlEncodedContent(data);

            _httpClient.DefaultRequestHeaders.Remove("X-CSRF-Token");
            _httpClient.DefaultRequestHeaders.Add("X-CSRF-Token", _csrfToken);
            _httpClient.DefaultRequestHeaders.Remove("Authorization");
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {authToken}");

            try
            {
                return await _vehicleIssuesRetryPolicy.ExecuteAsync(async () =>
                {
                    Console.WriteLine($"GetVehicleIssuesAsync: Sending POST request with CSRF={_csrfToken}, vehicle_id={vehicleId}, auth_token={authToken}");
                    LogMessage($"GetVehicleIssuesAsync: Sending POST request with CSRF token present={!string.IsNullOrEmpty(_csrfToken)}, vehicle_id={vehicleId}");
                    var response = await _httpClient.PostAsync("/includes/hiatme_config.php", content);
                    Console.WriteLine($"GetVehicleIssuesAsync: StatusCode={response.StatusCode}, Reason={response.ReasonPhrase}");
                    LogMessage($"GetVehicleIssuesAsync: StatusCode={response.StatusCode}");

                    var json = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"GetVehicleIssuesAsync response: {json}");
                    LogMessage($"GetVehicleIssuesAsync: Response JSON={json}");

                    if (string.IsNullOrWhiteSpace(json))
                    {
                        Console.WriteLine("GetVehicleIssuesAsync: Empty response received.");
                        LogMessage("GetVehicleIssuesAsync: Empty response received.");
                        return (false, null, "Empty response from server.");
                    }

                    VehicleIssuesResponse? result = await Task.Run(() => JsonConvert.DeserializeObject<VehicleIssuesResponse>(json));
                    if (result == null)
                    {
                        Console.WriteLine("GetVehicleIssuesAsync: Deserialized response is null.");
                        LogMessage("GetVehicleIssuesAsync: Deserialized response is null.");
                        return (false, null, "Invalid response format from server.");
                    }

                    // Check for errors - always check with server first for token errors
                    if (!result.Success)
                    {
                        string errorMsg = result.Message ?? "Unknown error";
                        string lowerError = errorMsg.ToLowerInvariant();
                        
                        // If server explicitly says logged in elsewhere, handle it
                        if (errorMsg.StartsWith("LOGGED_IN_ELSEWHERE:", StringComparison.OrdinalIgnoreCase))
                        {
                            string actualMessage = errorMsg.Substring("LOGGED_IN_ELSEWHERE:".Length);
                            LogMessage($"GetVehicleIssuesAsync: Server reported logged in elsewhere");
                            _ = HandleLoggedInElsewhereAsync(actualMessage);
                            return (false, null, $"LOGGED_IN_ELSEWHERE:{actualMessage}");
                        }
                        
                        // If it's a token/auth error, check with server first before showing error popup
                        bool isTokenError = lowerError.Contains("invalid token") || 
                                          lowerError.Contains("expired") || 
                                          lowerError.Contains("unauthorized") || 
                                          lowerError.Contains("forbidden") ||
                                          lowerError.Contains("invalid csrf") ||
                                          lowerError.Contains("authentication token") ||
                                          lowerError.Contains("unverified user");
                        
                        if (isTokenError)
                        {
                            LogMessage($"GetVehicleIssuesAsync: Token/auth error detected, checking with server first");
                            bool loggedInElsewhere = await CheckLoggedInElsewhereWithServerAsync(errorMsg);
                            if (loggedInElsewhere)
                            {
                                // HandleLoggedInElsewhereAsync already called in CheckLoggedInElsewhereWithServerAsync
                                return (false, null, "LOGGED_IN_ELSEWHERE:Session ended. You have been logged out because someone logged into your account from another device or browser.");
                            }
                        }
                        
                        // If CSRF token error, try fetching a new token and retrying once
                        if (lowerError.Contains("csrf token") || lowerError.Contains("invalid csrf") || lowerError.Contains("session token"))
                        {
                            LogMessage($"GetVehicleIssuesAsync: CSRF token error detected: {errorMsg}, fetching new token and retrying");
                            if (await FetchCSRFTokenAsync())
                            {
                                LogMessage($"GetVehicleIssuesAsync: Fetched new CSRF token={_csrfToken}, retrying");
                                _httpClient.DefaultRequestHeaders.Remove("X-CSRF-Token");
                                _httpClient.DefaultRequestHeaders.Add("X-CSRF-Token", _csrfToken);
                                
                                var retryResponse = await _httpClient.PostAsync("/includes/hiatme_config.php", content);
                                var retryJson = await retryResponse.Content.ReadAsStringAsync();
                                LogMessage($"GetVehicleIssuesAsync: Retry response StatusCode={retryResponse.StatusCode}, JSON={retryJson}");
                                
                                var retryResult = await Task.Run(() => JsonConvert.DeserializeObject<VehicleIssuesResponse>(retryJson));
                                
                                if (retryResult?.Success == true)
                                {
                                    // Update token from retry response
                                    if (!string.IsNullOrEmpty(retryResult.CsrfToken))
                                    {
                                        _csrfToken = retryResult.CsrfToken;
                                        Preferences.Set("CSRFToken", _csrfToken);
                                        LogMessage($"GetVehicleIssuesAsync: Updated CSRF token from retry: {_csrfToken}");
                                    }
                                    Console.WriteLine($"GetVehicleIssuesAsync: Successfully fetched {retryResult.Issues?.Count ?? 0} issues after retry");
                                    return (true, retryResult.Issues, "Issues fetched successfully.");
                                }
                                else
                                {
                                    // Check again for logged in elsewhere on retry failure
                                    if (retryResult != null && retryResult.Message?.StartsWith("LOGGED_IN_ELSEWHERE:", StringComparison.OrdinalIgnoreCase) == true)
                                    {
                                        string actualMessage = retryResult.Message.Substring("LOGGED_IN_ELSEWHERE:".Length);
                                        _ = HandleLoggedInElsewhereAsync(actualMessage);
                                        return (false, null, $"LOGGED_IN_ELSEWHERE:{actualMessage}");
                                    }
                                    
                                    LogMessage($"GetVehicleIssuesAsync: Retry also failed: {retryResult?.Message ?? "Unknown error"}");
                                    return (false, null, retryResult?.Message ?? "Failed to fetch issues after retry.");
                                }
                            }
                            else
                            {
                                LogMessage("GetVehicleIssuesAsync: Failed to fetch new CSRF token for retry");
                                return (false, null, "Failed to retrieve session token. Please try again.");
                            }
                        }
                    }

                    if (!string.IsNullOrEmpty(result.CsrfToken))
                    {
                        _csrfToken = result.CsrfToken;
                        Preferences.Set("CSRFToken", _csrfToken);
                        Console.WriteLine($"GetVehicleIssuesAsync: Updated CSRF token: {_csrfToken}");
                        LogMessage($"GetVehicleIssuesAsync: Updated CSRF token: {_csrfToken}");
                    }

                    if (result.Success)
                    {
                        // Record successful API call timestamp
                        RecordSuccessfulApiCall();
                        
                        Console.WriteLine($"GetVehicleIssuesAsync: Successfully fetched {result.Issues?.Count ?? 0} issues for vehicle_id={vehicleId}");
                        LogMessage($"GetVehicleIssuesAsync: Successfully fetched {result.Issues?.Count ?? 0} issues");
                        return (true, result.Issues, "Issues fetched successfully.");
                    }
                    else
                    {
                        Console.WriteLine($"GetVehicleIssuesAsync failed: {result.Message ?? "Unknown error"}");
                        LogMessage($"GetVehicleIssuesAsync failed: {result.Message ?? "Unknown error"}");
                        return (false, null, result.Message ?? "Failed to fetch issues.");
                    }
                });
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine($"GetVehicleIssuesAsync: HTTP error: {ex.Message}, StackTrace: {ex.StackTrace}");
                LogMessage($"GetVehicleIssuesAsync: HTTP error: {ex.Message}");
                return (false, null, $"Network error: {ex.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"GetVehicleIssuesAsync: Unexpected error: {ex.Message}, StackTrace: {ex.StackTrace}");
                LogMessage($"GetVehicleIssuesAsync: Unexpected error: {ex.Message}");
                return (false, null, $"An error occurred: {ex.Message}");
            }
        }

        public async Task<(bool Success, string Message)> AddVehicleIssueAsync(int vehicleId, string issueType, string? description)
        {
            Console.WriteLine($"AddVehicleIssueAsync: Starting for vehicle_id={vehicleId}, issue_type={issueType}");
            LogMessage($"AddVehicleIssueAsync: Starting for vehicle_id={vehicleId}, issue_type={issueType}");
            
            // Use stored token from Preferences (from last successful response)
            var storedToken = Preferences.Get("CSRFToken", null);
            if (!string.IsNullOrEmpty(storedToken))
            {
                _csrfToken = storedToken;
                LogMessage($"AddVehicleIssueAsync: Using CSRF token from last response (Preferences): {_csrfToken}");
            }
            else if (string.IsNullOrEmpty(_csrfToken))
            {
                // Only fetch if we have NO token at all
                LogMessage("AddVehicleIssueAsync: No token found, fetching fresh CSRF token");
                if (!await FetchCSRFTokenAsync())
                {
                    Console.WriteLine("AddVehicleIssueAsync: Failed to retrieve CSRF token.");
                    LogMessage("AddVehicleIssueAsync: Failed to retrieve CSRF token.");
                    return (false, "Failed to retrieve session token.");
                }
            }

            var authToken = Preferences.Get("AuthToken", null);
            if (string.IsNullOrEmpty(authToken))
            {
                Console.WriteLine("AddVehicleIssueAsync: No auth_token found in Preferences.");
                LogMessage("AddVehicleIssueAsync: No auth_token found in Preferences.");
                return (false, "No authentication token available. Please log in again.");
            }

            var data = new Dictionary<string, string>
        {
            { "action", "add_vehicle_issue" },
            { "vehicle_id", vehicleId.ToString() },
            { "issue_type", issueType },
            { "auth_token", authToken }
        };
            if (!string.IsNullOrEmpty(description))
            {
                data.Add("description", description);
            }
            var content = new FormUrlEncodedContent(data);

            _httpClient.DefaultRequestHeaders.Remove("X-CSRF-Token");
            _httpClient.DefaultRequestHeaders.Add("X-CSRF-Token", _csrfToken);
            _httpClient.DefaultRequestHeaders.Remove("Authorization");
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {authToken}");

            try
            {
                return await _genericRetryPolicy.ExecuteAsync(async () =>
                {
                    Console.WriteLine($"AddVehicleIssueAsync: Sending POST request with CSRF={_csrfToken}, vehicle_id={vehicleId}, issue_type={issueType}, auth_token={authToken}");
                    LogMessage($"AddVehicleIssueAsync: Sending POST request with CSRF token present={!string.IsNullOrEmpty(_csrfToken)}, vehicle_id={vehicleId}");
                    var response = await _httpClient.PostAsync("/includes/hiatme_config.php", content);
                    Console.WriteLine($"AddVehicleIssueAsync: StatusCode={response.StatusCode}, Reason={response.ReasonPhrase}");
                    LogMessage($"AddVehicleIssueAsync: StatusCode={response.StatusCode}");

                    var json = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"AddVehicleIssueAsync response: {json}");
                    LogMessage($"AddVehicleIssueAsync: Response JSON={json}");

                    if (string.IsNullOrWhiteSpace(json))
                    {
                        Console.WriteLine("AddVehicleIssueAsync: Empty response received.");
                        LogMessage("AddVehicleIssueAsync: Empty response received.");
                        return (false, "Empty response from server.");
                    }

                    GenericResponse? result = await Task.Run(() => JsonConvert.DeserializeObject<GenericResponse>(json));
                    if (result == null)
                    {
                        Console.WriteLine("AddVehicleIssueAsync: Deserialized response is null.");
                        LogMessage("AddVehicleIssueAsync: Deserialized response is null.");
                        return (false, "Invalid response format from server.");
                    }

                    // Check for errors - always check with server first for token errors
                    if (!result.Success)
                    {
                        string errorMsg = result.Message ?? "Unknown error";
                        string lowerError = errorMsg.ToLowerInvariant();
                        
                        // If server explicitly says logged in elsewhere, handle it
                        if (errorMsg.StartsWith("LOGGED_IN_ELSEWHERE:", StringComparison.OrdinalIgnoreCase))
                        {
                            string actualMessage = errorMsg.Substring("LOGGED_IN_ELSEWHERE:".Length);
                            LogMessage($"AddVehicleIssueAsync: Server reported logged in elsewhere");
                            _ = HandleLoggedInElsewhereAsync(actualMessage);
                            return (false, $"LOGGED_IN_ELSEWHERE:{actualMessage}");
                        }
                        
                        // If it's a token/auth error, check with server first before showing error popup
                        bool isTokenError = lowerError.Contains("invalid token") || 
                                          lowerError.Contains("expired") || 
                                          lowerError.Contains("unauthorized") || 
                                          lowerError.Contains("forbidden") ||
                                          lowerError.Contains("invalid csrf") ||
                                          lowerError.Contains("authentication token") ||
                                          lowerError.Contains("unverified user");
                        
                        if (isTokenError)
                        {
                            LogMessage($"AddVehicleIssueAsync: Token/auth error detected, checking with server first");
                            bool loggedInElsewhere = await CheckLoggedInElsewhereWithServerAsync(errorMsg);
                            if (loggedInElsewhere)
                            {
                                // HandleLoggedInElsewhereAsync already called in CheckLoggedInElsewhereWithServerAsync
                                return (false, "LOGGED_IN_ELSEWHERE:Session ended. You have been logged out because someone logged into your account from another device or browser.");
                            }
                        }
                        
                        // If CSRF token error, try fetching a new token and retrying once
                        if (lowerError.Contains("csrf token") || lowerError.Contains("invalid csrf") || lowerError.Contains("session token"))
                        {
                            LogMessage($"AddVehicleIssueAsync: CSRF token error detected: {errorMsg}, fetching new token and retrying");
                            if (await FetchCSRFTokenAsync())
                            {
                                LogMessage($"AddVehicleIssueAsync: Fetched new CSRF token={_csrfToken}, retrying");
                                _httpClient.DefaultRequestHeaders.Remove("X-CSRF-Token");
                                _httpClient.DefaultRequestHeaders.Add("X-CSRF-Token", _csrfToken);
                                
                                var retryResponse = await _httpClient.PostAsync("/includes/hiatme_config.php", content);
                                var retryJson = await retryResponse.Content.ReadAsStringAsync();
                                LogMessage($"AddVehicleIssueAsync: Retry response StatusCode={retryResponse.StatusCode}, JSON={retryJson}");
                                
                                var retryResult = await Task.Run(() => JsonConvert.DeserializeObject<GenericResponse>(retryJson));
                                
                                if (retryResult?.Success == true)
                                {
                                    // Update token from retry response
                                    if (!string.IsNullOrEmpty(retryResult.CsrfToken))
                                    {
                                        _csrfToken = retryResult.CsrfToken;
                                        Preferences.Set("CSRFToken", _csrfToken);
                                        LogMessage($"AddVehicleIssueAsync: Updated CSRF token from retry: {_csrfToken}");
                                    }
                                    Console.WriteLine($"AddVehicleIssueAsync: Successfully added issue after retry");
                                    return (true, retryResult.Message ?? "Issue added successfully.");
                                }
                                else
                                {
                                    // Check again for logged in elsewhere on retry failure
                                    if (retryResult != null && retryResult.Message?.StartsWith("LOGGED_IN_ELSEWHERE:", StringComparison.OrdinalIgnoreCase) == true)
                                    {
                                        string actualMessage = retryResult.Message.Substring("LOGGED_IN_ELSEWHERE:".Length);
                                        _ = HandleLoggedInElsewhereAsync(actualMessage);
                                        return (false, $"LOGGED_IN_ELSEWHERE:{actualMessage}");
                                    }
                                    
                                    LogMessage($"AddVehicleIssueAsync: Retry also failed: {retryResult?.Message ?? "Unknown error"}");
                                    return (false, retryResult?.Message ?? "Failed to add issue after retry.");
                                }
                            }
                            else
                            {
                                LogMessage("AddVehicleIssueAsync: Failed to fetch new CSRF token for retry");
                                return (false, "Failed to retrieve session token. Please try again.");
                            }
                        }
                    }

                    if (!string.IsNullOrEmpty(result.CsrfToken))
                    {
                        _csrfToken = result.CsrfToken;
                        Preferences.Set("CSRFToken", _csrfToken);
                        Console.WriteLine($"AddVehicleIssueAsync: Updated CSRF token: {_csrfToken}");
                        LogMessage($"AddVehicleIssueAsync: Updated CSRF token: {_csrfToken}");
                    }

                    if (result.Success)
                    {
                        // Record successful API call timestamp
                        RecordSuccessfulApiCall();
                        
                        Console.WriteLine($"AddVehicleIssueAsync: Successfully added issue for vehicle_id={vehicleId}, issue_type={issueType}");
                        LogMessage($"AddVehicleIssueAsync: Successfully added issue");
                        return (true, result.Message ?? "Issue added successfully.");
                    }
                    else
                    {
                        Console.WriteLine($"AddVehicleIssueAsync failed: {result.Message ?? "Unknown error"}");
                        LogMessage($"AddVehicleIssueAsync failed: {result.Message ?? "Unknown error"}");
                        return (false, result.Message ?? "Failed to add issue.");
                    }
                });
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine($"AddVehicleIssueAsync: HTTP error: {ex.Message}, StackTrace: {ex.StackTrace}");
                LogMessage($"AddVehicleIssueAsync: HTTP error: {ex.Message}");
                return (false, $"Network error: {ex.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"AddVehicleIssueAsync: Unexpected error: {ex.Message}, StackTrace: {ex.StackTrace}");
                LogMessage($"AddVehicleIssueAsync: Unexpected error: {ex.Message}");
                return (false, $"An error occurred: {ex.Message}");
            }
        }

        private class CsrfResponse
        {
            public bool Success { get; set; }
            [JsonProperty("csrf_token")]
            public string? CsrfToken { get; set; }
            public string? Message { get; set; }
        }

        private class LoginResponse
        {
            public bool Success { get; set; }
            public string? Email { get; set; }
            public string? Name { get; set; }
            public string? Phone { get; set; }
            [JsonProperty("profile_picture")]
            public string? ProfilePicture { get; set; }
            [JsonProperty("csrf_token")]
            public string? CsrfToken { get; set; }
            public string? Message { get; set; }
            public string? Role { get; set; }
            [JsonProperty("user_id")]
            public int UserId { get; set; }
            public List<Vehicle>? Vehicles { get; set; }
            [JsonProperty("auth_token")]
            public string? AuthToken { get; set; }
        }

        private class GenericResponse
        {
            public bool Success { get; set; }
            public string? Message { get; set; }
            [JsonProperty("csrf_token")]
            public string? CsrfToken { get; set; }
        }

        private class AssignVehicleResponse
        {
            public bool Success { get; set; }
            public string? Message { get; set; }
            [JsonProperty("csrf_token")]
            public string? CsrfToken { get; set; }
            public Vehicle? Vehicle { get; set; }
            [JsonProperty("incomplete_records")]
            public List<MileageRecord>? IncompleteRecords { get; set; }
            [JsonProperty("mileage_id")]
            public int? MileageId { get; set; }
        }
        
        private class SubmitMileageResponse
        {
            public bool Success { get; set; }
            public string? Message { get; set; }
            [JsonProperty("csrf_token")]
            public string? CsrfToken { get; set; }
            [JsonProperty("mileage_id")]
            public int? MileageId { get; set; }
            [JsonProperty("vehicle_id")]
            public int VehicleId { get; set; }
            [JsonProperty("user_id")]
            public int UserId { get; set; }
            [JsonProperty("start_miles")]
            public float? StartMiles { get; set; }
            [JsonProperty("start_miles_datetime")]
            public string? StartMilesDatetime { get; set; }
            [JsonProperty("ending_miles")]
            public float? EndingMiles { get; set; }
            [JsonProperty("ending_miles_datetime")]
            public string? EndingMilesDatetime { get; set; }
            [JsonProperty("created_at")]
            public string? CreatedAt { get; set; }
        }

        private class VehicleIssuesResponse
        {
            public bool Success { get; set; }
            public List<VehicleIssue>? Issues { get; set; }
            [JsonProperty("csrf_token")]
            public string? CsrfToken { get; set; }
            public string? Message { get; set; }
        }

        public async Task<(bool Success, Models.User? User, string Message)> UpdateProfileAsync(string currentEmail, string name, string email, string? phone, FileResult? profilePicture)
        {
            Console.WriteLine($"UpdateProfileAsync: Starting for Email={email}");
            if (string.IsNullOrEmpty(_csrfToken) && !await FetchCSRFTokenAsync())
            {
                Console.WriteLine("UpdateProfileAsync: Failed to retrieve CSRF token.");
                return (false, null, "Failed to retrieve session token.");
            }

            if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(email) || string.IsNullOrEmpty(currentEmail))
            {
                Console.WriteLine("UpdateProfileAsync: Missing required fields.");
                return (false, null, "Name, email, and current email are required.");
            }

            var authToken = Preferences.Get("AuthToken", null);
            if (string.IsNullOrEmpty(authToken))
            {
                Console.WriteLine("UpdateProfileAsync: No auth_token found in Preferences.");
                return (false, null, "No authentication token available. Please log in again.");
            }

            try
            {
                return await _updateProfileRetryPolicy.ExecuteAsync(async () =>
                {
                    using var multipartContent = new MultipartFormDataContent();
                    
                    multipartContent.Add(new StringContent("update_profile"), "action");
                    multipartContent.Add(new StringContent(currentEmail), "current_email");
                    multipartContent.Add(new StringContent(name), "name");
                    multipartContent.Add(new StringContent(email), "email");
                    multipartContent.Add(new StringContent(phone ?? string.Empty), "phone");

                    // Add profile picture if provided
                    if (profilePicture != null)
                    {
                        try
                        {
                            var fileStream = await profilePicture.OpenReadAsync();
                            var streamContent = new StreamContent(fileStream);
                            streamContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(profilePicture.ContentType ?? "image/jpeg");
                            multipartContent.Add(streamContent, "profile_picture", profilePicture.FileName ?? "profile.jpg");
                            Console.WriteLine($"UpdateProfileAsync: Added profile picture: {profilePicture.FileName}, ContentType: {profilePicture.ContentType}");
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"UpdateProfileAsync: Error reading profile picture: {ex.Message}");
                            return (false, null, "Failed to read profile picture.");
                        }
                    }

                    _httpClient.DefaultRequestHeaders.Remove("X-CSRF-Token");
                    _httpClient.DefaultRequestHeaders.Add("X-CSRF-Token", _csrfToken);
                    _httpClient.DefaultRequestHeaders.Remove("Authorization");
                    _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {authToken}");

                    Console.WriteLine($"UpdateProfileAsync: Sending POST request with CSRF={_csrfToken}, currentEmail={currentEmail}, email={email}, name={name}, hasPicture={profilePicture != null}");
                    var response = await _httpClient.PostAsync("/includes/hiatme_config.php", multipartContent);
                    Console.WriteLine($"UpdateProfileAsync: StatusCode={response.StatusCode}, Reason={response.ReasonPhrase}");

                    var json = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"UpdateProfileAsync response: {json}");

                    if (string.IsNullOrWhiteSpace(json))
                    {
                        Console.WriteLine("UpdateProfileAsync: Empty response received.");
                        return (false, null, "Empty response from server.");
                    }

                    UpdateProfileResponse? result = await Task.Run(() => JsonConvert.DeserializeObject<UpdateProfileResponse>(json, new JsonSerializerSettings
                    {
                        MissingMemberHandling = MissingMemberHandling.Ignore,
                        NullValueHandling = NullValueHandling.Ignore
                    }));

                    if (result == null)
                    {
                        Console.WriteLine("UpdateProfileAsync: Deserialized response is null.");
                        return (false, null, "Invalid response format from server.");
                    }

                    if (!string.IsNullOrEmpty(result.CsrfToken))
                    {
                        _csrfToken = result.CsrfToken;
                        Preferences.Set("CSRFToken", _csrfToken);
                        Console.WriteLine($"UpdateProfileAsync: Updated CSRF token: {_csrfToken}");
                        LogMessage($"UpdateProfileAsync: Updated CSRF token: {_csrfToken}");
                    }

                    if (result.Success && result.User != null)
                    {
                        var updatedUser = new Models.User
                        {
                            Email = result.User.Email ?? email,
                            Name = result.User.Name ?? name,
                            Phone = result.User.Phone ?? phone,
                            ProfilePicture = result.User.ProfilePicture,
                            Role = App.CurrentUser?.Role, // Preserve existing role
                            UserId = App.CurrentUser?.UserId ?? 0, // Preserve existing user ID
                            Vehicles = App.CurrentUser?.Vehicles // Preserve existing vehicles
                        };
                        App.CurrentUser = updatedUser;
                        Preferences.Set("UserData", JsonConvert.SerializeObject(updatedUser));
                        Console.WriteLine($"UpdateProfileAsync: Profile updated successfully for Email={email}");
                        return (true, updatedUser, result.Message ?? "Profile updated successfully.");
                    }
                    else
                    {
                        Console.WriteLine($"UpdateProfileAsync failed: {result.Message ?? "Unknown error"}");
                        return (false, null, result.Message ?? "Failed to update profile.");
                    }
                });
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine($"UpdateProfileAsync: HTTP error: {ex.Message}, StackTrace: {ex.StackTrace}");
                return (false, null, $"Network error: {ex.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"UpdateProfileAsync: Unexpected error: {ex.Message}, StackTrace: {ex.StackTrace}");
                return (false, null, $"An error occurred: {ex.Message}");
            }
        }

        public async Task<(bool Success, string Message)> SubmitDayOffRequestAsync(DateTime date, string? reason, TimeSpan? startTime = null, TimeSpan? endTime = null)
        {
            Console.WriteLine($"SubmitDayOffRequestAsync: Starting for date={date:yyyy-MM-dd}");
            if (string.IsNullOrEmpty(_csrfToken) && !await FetchCSRFTokenAsync())
            {
                Console.WriteLine("SubmitDayOffRequestAsync: Failed to retrieve CSRF token.");
                return (false, "Failed to retrieve session token.");
            }

            var authToken = Preferences.Get("AuthToken", null);
            if (string.IsNullOrEmpty(authToken))
            {
                Console.WriteLine("SubmitDayOffRequestAsync: No auth_token found in Preferences.");
                return (false, "No authentication token available. Please log in again.");
            }

            var easternTime = TimeZoneInfo.ConvertTime(date, TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time"));
            var data = new Dictionary<string, string>
            {
                { "action", "request_day_off" },
                { "request_date", easternTime.ToString("yyyy-MM-dd") },
                { "auth_token", authToken }
            };

            if (!string.IsNullOrWhiteSpace(reason))
            {
                data.Add("reason", reason);
            }

            if (startTime.HasValue && endTime.HasValue)
            {
                // Only send time range if both are provided; server will treat missing times as full day
                var start = startTime.Value;
                var end = endTime.Value;
                data.Add("start_time", new DateTime(1, 1, 1, start.Hours, start.Minutes, 0).ToString("HH:mm:ss"));
                data.Add("end_time", new DateTime(1, 1, 1, end.Hours, end.Minutes, 0).ToString("HH:mm:ss"));
            }

            var content = new FormUrlEncodedContent(data);

            _httpClient.DefaultRequestHeaders.Remove("X-CSRF-Token");
            _httpClient.DefaultRequestHeaders.Add("X-CSRF-Token", _csrfToken);
            _httpClient.DefaultRequestHeaders.Remove("Authorization");
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {authToken}");

            try
            {
                return await _dayOffRequestRetryPolicy.ExecuteAsync(async () =>
                {
                    Console.WriteLine($"SubmitDayOffRequestAsync: Sending POST request with CSRF={_csrfToken}, date={data["request_date"]}, auth_token={authToken}");
                    var response = await _httpClient.PostAsync("/includes/hiatme_config.php", content);
                    Console.WriteLine($"SubmitDayOffRequestAsync: StatusCode={response.StatusCode}, Reason={response.ReasonPhrase}");

                    var json = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"SubmitDayOffRequestAsync response: {json}");

                    if (string.IsNullOrWhiteSpace(json))
                    {
                        Console.WriteLine("SubmitDayOffRequestAsync: Empty response received.");
                        return (false, "Empty response from server.");
                    }

                    GenericResponse? result = await Task.Run(() => JsonConvert.DeserializeObject<GenericResponse>(json));
                    if (result == null)
                    {
                        Console.WriteLine("SubmitDayOffRequestAsync: Deserialized response is null.");
                        return (false, "Invalid response format from server.");
                    }

                    if (!string.IsNullOrEmpty(result.CsrfToken))
                    {
                        _csrfToken = result.CsrfToken;
                        Preferences.Set("CSRFToken", _csrfToken);
                        Console.WriteLine($"SubmitDayOffRequestAsync: Updated CSRF token: {_csrfToken}");
                        LogMessage($"SubmitDayOffRequestAsync: Updated CSRF token: {_csrfToken}");
                    }

                    if (result.Success)
                    {
                        Console.WriteLine("SubmitDayOffRequestAsync: Request submitted successfully.");
                        return (true, result.Message ?? "Request submitted successfully.");
                    }
                    else
                    {
                        Console.WriteLine($"SubmitDayOffRequestAsync failed: {result.Message ?? "Unknown error"}");
                        return (false, result.Message ?? "Failed to submit request.");
                    }
                });
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine($"SubmitDayOffRequestAsync: HTTP error: {ex.Message}, StackTrace: {ex.StackTrace}");
                return (false, $"Network error: {ex.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"SubmitDayOffRequestAsync: Unexpected error: {ex.Message}, StackTrace: {ex.StackTrace}");
                return (false, $"An error occurred: {ex.Message}");
            }
        }

        private class UpdateProfileResponse
        {
            public bool Success { get; set; }
            public string? Message { get; set; }
            [JsonProperty("csrf_token")]
            public string? CsrfToken { get; set; }
            public UpdateProfileUser? User { get; set; }
        }

        private class UpdateProfileUser
        {
            public string? Email { get; set; }
            public string? Name { get; set; }
            public string? Phone { get; set; }
            [JsonProperty("profile_picture")]
            public string? ProfilePicture { get; set; }
        }
        
        private void LogMessage(string message)
        {
            try
            {
                // Try multiple locations to ensure we can write
                string? logPath = null;
                
                // Try cache directory first (more accessible)
                try
                {
                    logPath = Path.Combine(FileSystem.CacheDirectory, "auth_service_log.txt");
                }
                catch
                {
                    // Fallback to app data directory
                    try
                    {
                        logPath = Path.Combine(FileSystem.AppDataDirectory, "auth_service_log.txt");
                    }
                    catch
                    {
                        // Last resort - try temp path
                        logPath = Path.Combine(Path.GetTempPath(), "auth_service_log.txt");
                    }
                }
                
                var logEntry = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}\n";
                File.AppendAllText(logPath, logEntry);
                System.Diagnostics.Debug.WriteLine($"AUTH LOG: {message}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"AUTH LOG ERROR: {ex.Message}");
            }
        }
    }
}