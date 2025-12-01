using HiatMeApp.Models;
using Newtonsoft.Json;
using Polly;
using Polly.Retry;
using System;
using System.Collections.Generic;
using System.Net.Http;
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

        public AuthService(HttpClient httpClient)
        {
            //boo
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _httpClient.Timeout = TimeSpan.FromSeconds(200);
            _csrfToken = string.Empty;
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "HiatMeApp/1.0");
            _httpClient.DefaultRequestHeaders.Add("Connection", "close"); // Prevent keep-alive issues
            Console.WriteLine("AuthService initialized with BaseAddress: " + _httpClient.BaseAddress);

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
        }

        public async Task<bool> FetchCSRFTokenAsync()
        {
            try
            {
                string endpoint = "/includes/hiatme_config.php?action=get_csrf_token";
                Console.WriteLine($"Fetching CSRF token from: {_httpClient.BaseAddress}{endpoint}");
                var response = await _httpClient.GetAsync(endpoint);
                Console.WriteLine($"FetchCSRFTokenAsync: StatusCode={response.StatusCode}, Reason={response.ReasonPhrase}");

                if (!response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"FetchCSRFTokenAsync failed: Status={response.StatusCode}, Reason={response.ReasonPhrase}");
                    return false;
                }

                var json = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"FetchCSRFTokenAsync response: {json}");

                if (string.IsNullOrWhiteSpace(json))
                {
                    Console.WriteLine("FetchCSRFTokenAsync: Empty response received.");
                    return false;
                }

                var result = await Task.Run(() => JsonConvert.DeserializeObject<CsrfResponse>(json));
                if (result?.Success == true && !string.IsNullOrEmpty(result.CsrfToken))
                {
                    _csrfToken = result.CsrfToken;
                    Console.WriteLine($"FetchCSRFTokenAsync: CSRF token fetched successfully: {_csrfToken}");
                    return true;
                }
                else
                {
                    Console.WriteLine($"FetchCSRFTokenAsync: Invalid response. Success={result?.Success}, Token={result?.CsrfToken}");
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

        public async Task<(bool Success, User? User, string Message)> LoginAsync(string? email, string? password)
        {
            if (string.IsNullOrEmpty(_csrfToken) && !await FetchCSRFTokenAsync())
                return (false, null, "Failed to retrieve session token.");

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
                    var response = await _httpClient.PostAsync("/includes/hiatme_config.php", content);
                    Console.WriteLine($"LoginAsync: StatusCode={response.StatusCode}, Reason={response.ReasonPhrase}");
                    Console.WriteLine("LoginAsync: Response headers:");
                    foreach (var header in response.Headers)
                    {
                        Console.WriteLine($"LoginAsync: {header.Key}: {string.Join(", ", header.Value)}");
                    }

                    var json = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"LoginAsync raw response: {json}");

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
                        Console.WriteLine($"LoginAsync: Updated CSRF token: {_csrfToken}");
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
                        Console.WriteLine($"LoginAsync: Login successful for Email={email}, Role={result.Role}, UserId={result.UserId}, VehiclesCount={result.Vehicles?.Count ?? 0}, AuthToken={result.AuthToken}");
                        Console.WriteLine($"LoginAsync: ProfilePicture from server: '{result.ProfilePicture ?? "null"}' (length: {result.ProfilePicture?.Length ?? 0})");
                        Console.WriteLine($"LoginAsync: User.ProfilePicture after assignment: '{user.ProfilePicture ?? "null"}' (length: {user.ProfilePicture?.Length ?? 0})");
                        return (true, user, "Login successful");
                    }
                    else
                    {
                        Console.WriteLine($"LoginAsync failed: {result?.Message ?? "Unknown error"}");
                        return (false, null, result?.Message ?? "Login failed.");
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
                        Console.WriteLine($"RegisterAsync: Updated CSRF token: {_csrfToken}");
                    }

                    if (result.Success)
                    {
                        Console.WriteLine($"RegisterAsync: Registration successful for Email={email}");
                        return (true, result.Message ?? "Registration successful.");
                    }
                    else
                    {
                        Console.WriteLine($"RegisterAsync failed: {result.Message ?? "Unknown error"}");
                        return (false, result.Message ?? "Registration failed.");
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
                        Console.WriteLine($"ForgotPasswordAsync: Updated CSRF token: {_csrfToken}");
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
                        Console.WriteLine($"AssignVehicleAsync: Updated CSRF token: {_csrfToken}");
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
            if (string.IsNullOrEmpty(_csrfToken) && !await FetchCSRFTokenAsync())
                return (false, "Failed to retrieve session token.", null);

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
                    var response = await _httpClient.PostAsync("/includes/hiatme_config.php", content);
                    Console.WriteLine($"SubmitStartMileageAsync: StatusCode={response.StatusCode}, Reason={response.ReasonPhrase}");
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
                        _csrfToken = result.CsrfToken;

                    Console.WriteLine($"SubmitStartMileageAsync: Updated CSRF token: {_csrfToken}");
                    Console.WriteLine($"SubmitStartMileageAsync: Successfully submitted start mileage for vehicle_id={result.VehicleId}, mileage_id={result.MileageId}");
                    return (result.Success, result.Message ?? "Failed to submit start mileage.", result.MileageId);
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
                        Console.WriteLine($"SubmitEndMileageAsync: Updated CSRF token: {_csrfToken}");
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
            if (string.IsNullOrEmpty(_csrfToken) && !await FetchCSRFTokenAsync())
            {
                Console.WriteLine("GetVehicleIssuesAsync: Failed to retrieve CSRF token.");
                return (false, null, "Failed to retrieve session token.");
            }

            var authToken = Preferences.Get("AuthToken", null);
            if (string.IsNullOrEmpty(authToken))
            {
                Console.WriteLine("GetVehicleIssuesAsync: No auth_token found in Preferences.");
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
                    var response = await _httpClient.PostAsync("/includes/hiatme_config.php", content);
                    Console.WriteLine($"GetVehicleIssuesAsync: StatusCode={response.StatusCode}, Reason={response.ReasonPhrase}");
                    Console.WriteLine("GetVehicleIssuesAsync: Response headers:");
                    foreach (var header in response.Headers)
                    {
                        Console.WriteLine($"GetVehicleIssuesAsync: {header.Key}: {string.Join(", ", header.Value)}");
                    }

                    var json = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"GetVehicleIssuesAsync response: {json}");

                    if (string.IsNullOrWhiteSpace(json))
                    {
                        Console.WriteLine("GetVehicleIssuesAsync: Empty response received.");
                        return (false, null, "Empty response from server.");
                    }

                    VehicleIssuesResponse? result = await Task.Run(() => JsonConvert.DeserializeObject<VehicleIssuesResponse>(json));
                    if (result == null)
                    {
                        Console.WriteLine("GetVehicleIssuesAsync: Deserialized response is null.");
                        return (false, null, "Invalid response format from server.");
                    }

                    if (!string.IsNullOrEmpty(result.CsrfToken))
                    {
                        _csrfToken = result.CsrfToken;
                        Console.WriteLine($"GetVehicleIssuesAsync: Updated CSRF token: {_csrfToken}");
                    }

                    if (result.Success)
                    {
                        Console.WriteLine($"GetVehicleIssuesAsync: Successfully fetched {result.Issues?.Count ?? 0} issues for vehicle_id={vehicleId}");
                        return (true, result.Issues, "Issues fetched successfully.");
                    }
                    else
                    {
                        Console.WriteLine($"GetVehicleIssuesAsync failed: {result.Message ?? "Unknown error"}");
                        return (false, null, result.Message ?? "Failed to fetch issues.");
                    }
                });
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine($"GetVehicleIssuesAsync: HTTP error: {ex.Message}, StackTrace: {ex.StackTrace}");
                return (false, null, $"Network error: {ex.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"GetVehicleIssuesAsync: Unexpected error: {ex.Message}, StackTrace: {ex.StackTrace}");
                return (false, null, $"An error occurred: {ex.Message}");
            }
        }

        public async Task<(bool Success, string Message)> AddVehicleIssueAsync(int vehicleId, string issueType, string? description)
        {
            Console.WriteLine($"AddVehicleIssueAsync: Starting for vehicle_id={vehicleId}, issue_type={issueType}");
            if (string.IsNullOrEmpty(_csrfToken) && !await FetchCSRFTokenAsync())
            {
                Console.WriteLine("AddVehicleIssueAsync: Failed to retrieve CSRF token.");
                return (false, "Failed to retrieve session token.");
            }

            var authToken = Preferences.Get("AuthToken", null);
            if (string.IsNullOrEmpty(authToken))
            {
                Console.WriteLine("AddVehicleIssueAsync: No auth_token found in Preferences.");
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
                    var response = await _httpClient.PostAsync("/includes/hiatme_config.php", content);
                    Console.WriteLine($"AddVehicleIssueAsync: StatusCode={response.StatusCode}, Reason={response.ReasonPhrase}");
                    Console.WriteLine("AddVehicleIssueAsync: Response headers:");
                    foreach (var header in response.Headers)
                    {
                        Console.WriteLine($"AddVehicleIssueAsync: {header.Key}: {string.Join(", ", header.Value)}");
                    }

                    var json = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"AddVehicleIssueAsync response: {json}");

                    if (string.IsNullOrWhiteSpace(json))
                    {
                        Console.WriteLine("AddVehicleIssueAsync: Empty response received.");
                        return (false, "Empty response from server.");
                    }

                    GenericResponse? result = await Task.Run(() => JsonConvert.DeserializeObject<GenericResponse>(json));
                    if (result == null)
                    {
                        Console.WriteLine("AddVehicleIssueAsync: Deserialized response is null.");
                        return (false, "Invalid response format from server.");
                    }

                    if (!string.IsNullOrEmpty(result.CsrfToken))
                    {
                        _csrfToken = result.CsrfToken;
                        Console.WriteLine($"AddVehicleIssueAsync: Updated CSRF token: {_csrfToken}");
                    }

                    if (result.Success)
                    {
                        Console.WriteLine($"AddVehicleIssueAsync: Successfully added issue for vehicle_id={vehicleId}, issue_type={issueType}");
                        return (true, result.Message ?? "Issue added successfully.");
                    }
                    else
                    {
                        Console.WriteLine($"AddVehicleIssueAsync failed: {result.Message ?? "Unknown error"}");
                        return (false, result.Message ?? "Failed to add issue.");
                    }
                });
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine($"AddVehicleIssueAsync: HTTP error: {ex.Message}, StackTrace: {ex.StackTrace}");
                return (false, $"Network error: {ex.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"AddVehicleIssueAsync: Unexpected error: {ex.Message}, StackTrace: {ex.StackTrace}");
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
                        Console.WriteLine($"UpdateProfileAsync: Updated CSRF token: {_csrfToken}");
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
    }
}