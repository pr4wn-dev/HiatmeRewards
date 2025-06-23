using HiatMeApp.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

namespace HiatMeApp.Services;

public class AuthService
{
    private readonly HttpClient _httpClient;
    private string? _csrfToken;

    public AuthService(HttpClient httpClient)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _csrfToken = string.Empty;
    }

    public async Task<bool> FetchCSRFTokenAsync()
    {
        try
        {
            Console.WriteLine("Fetching CSRF token from: " + _httpClient.BaseAddress + "/includes/hiatme_config.php?action=get_csrf_token");
            var response = await _httpClient.GetAsync("/includes/hiatme_config.php?action=get_csrf_token");
            Console.WriteLine($"Response status code: {response.StatusCode}");

            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine($"Failed to fetch CSRF token. Status: {response.StatusCode}, Reason: {response.ReasonPhrase}");
                return false;
            }

            var json = await response.Content.ReadAsStringAsync();
            Console.WriteLine($"Response content: {json}");

            if (string.IsNullOrWhiteSpace(json))
            {
                Console.WriteLine("Empty response received.");
                return false;
            }

            var result = JsonConvert.DeserializeObject<CsrfResponse>(json);
            if (result?.Success == true && !string.IsNullOrEmpty(result.CsrfToken))
            {
                _csrfToken = result.CsrfToken;
                Console.WriteLine($"CSRF token fetched successfully: {_csrfToken}");
                return true;
            }
            else
            {
                Console.WriteLine($"Invalid CSRF response. Success: {result?.Success}, Token: {result?.CsrfToken}");
                return false;
            }
        }
        catch (JsonException ex)
        {
            Console.WriteLine($"JSON parsing error: {ex.Message}");
            return false;
        }
        catch (HttpRequestException ex)
        {
            Console.WriteLine($"HTTP request error: {ex.Message}");
            return false;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Unexpected error in FetchCSRFTokenAsync: {ex.Message}");
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
            Console.WriteLine($"Sending login request for: {email} with CSRF token: {_csrfToken}");
            var response = await _httpClient.PostAsync("/includes/hiatme_config.php", content);
            Console.WriteLine($"Login response status code: {response.StatusCode}");

            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine($"Login request failed. Status: {response.StatusCode}, Reason: {response.ReasonPhrase}");
                // Fetch a fresh token on failure
                if (await FetchCSRFTokenAsync())
                    return (false, null, "Login failed. Please try again with new session token.");
                return (false, null, "Login failed and unable to refresh session token.");
            }

            var json = await response.Content.ReadAsStringAsync();
            Console.WriteLine($"Login response content: {json}");

            if (string.IsNullOrWhiteSpace(json))
            {
                Console.WriteLine("Empty login response received.");
                // Fetch a fresh token
                if (await FetchCSRFTokenAsync())
                    return (false, null, "Empty response. Please try again with new session token.");
                return (false, null, "Empty response and unable to refresh session token.");
            }

            LoginResponse? result = null;
            try
            {
                result = JsonConvert.DeserializeObject<LoginResponse>(json);
            }
            catch (JsonException ex)
            {
                Console.WriteLine($"JSON parsing error in LoginAsync: {ex.Message}");
                // Fetch a fresh token on deserialization failure
                if (await FetchCSRFTokenAsync())
                    return (false, null, "Invalid response format. Please try again with new session token.");
                return (false, null, "Invalid response format and unable to refresh session token.");
            }

            // Update CSRF token from response, if provided
            if (!string.IsNullOrEmpty(result?.CsrfToken))
            {
                _csrfToken = result.CsrfToken;
                Console.WriteLine($"Updated CSRF token: {_csrfToken}");
            }
            else
            {
                Console.WriteLine("No CSRF token in response. Fetching new token.");
                if (!await FetchCSRFTokenAsync())
                {
                    Console.WriteLine("Failed to fetch new CSRF token.");
                    return (false, null, "Unable to refresh session token.");
                }
                Console.WriteLine($"Fetched new CSRF token: {_csrfToken}");
            }

            if (result?.Success == true)
            {
                var user = new User
                {
                    Email = result.Email,
                    Name = result.Name,
                    Phone = result.Phone,
                    ProfilePicture = result.ProfilePicture
                };
                Console.WriteLine($"Login successful for: {email}");
                return (true, user, "Login successful");
            }
            else
            {
                Console.WriteLine($"Login failed: {result?.Message ?? "Unknown error"}");
                return (false, null, result?.Message ?? "Login failed.");
            }
        }
        catch (HttpRequestException ex)
        {
            Console.WriteLine($"HTTP request error in LoginAsync: {ex.Message}");
            // Fetch a fresh token on HTTP failure
            if (await FetchCSRFTokenAsync())
                return (false, null, "Network error. Please try again with new session token.");
            return (false, null, "Network error and unable to refresh session token.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Unexpected error in LoginAsync: {ex.Message}");
            // Fetch a fresh token on unexpected failure
            if (await FetchCSRFTokenAsync())
                return (false, null, "An error occurred. Please try again with new session token.");
            return (false, null, "An error occurred and unable to refresh session token.");
        }
    }

    public async Task<(bool Success, string Message)> RegisterAsync(string? name, string? email, string? phone, string? password)
    {
        if (string.IsNullOrEmpty(_csrfToken) && !await FetchCSRFTokenAsync())
            return (false, "Failed to retrieve session token.");

        if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
            return (false, "Name, email, and password are required.");

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
            Console.WriteLine($"Sending register request for: {email}");
            var response = await _httpClient.PostAsync("/includes/hiatme_config.php", content);
            var json = await response.Content.ReadAsStringAsync();
            Console.WriteLine($"Register response: {json}");

            var result = JsonConvert.DeserializeObject<GenericResponse>(json);
            if (result?.Success == true)
            {
                _csrfToken = result.CsrfToken;
                Console.WriteLine($"Registration successful for: {email}");
                return (true, result.Message ?? "Registration successful.");
            }
            else
            {
                _csrfToken = result?.CsrfToken ?? _csrfToken;
                Console.WriteLine($"Registration failed: {result?.Message}");
                return (false, result?.Message ?? "Registration failed.");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"RegisterAsync error: {ex.Message}");
            return (false, "An error occurred.");
        }
    }

    public async Task<(bool Success, string Message)> ForgotPasswordAsync(string? email)
    {
        if (string.IsNullOrEmpty(_csrfToken) && !await FetchCSRFTokenAsync())
            return (false, "Failed to retrieve session token.");

        if (string.IsNullOrEmpty(email))
            return (false, "Email is required.");

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
            Console.WriteLine($"Sending forgot password request for: {email}");
            var response = await _httpClient.PostAsync("/includes/hiatme_config.php", content);
            var json = await response.Content.ReadAsStringAsync();
            Console.WriteLine($"Forgot password response: {json}");

            var result = JsonConvert.DeserializeObject<GenericResponse>(json);
            if (result?.Success == true)
            {
                _csrfToken = result.CsrfToken;
                Console.WriteLine($"Forgot password request successful for: {email}");
                return (true, result.Message ?? "Reset link sent.");
            }
            else
            {
                _csrfToken = result?.CsrfToken ?? _csrfToken;
                Console.WriteLine($"Forgot password failed: {result?.Message}");
                return (false, result?.Message ?? "Failed to send reset link.");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"ForgotPasswordAsync error: {ex.Message}");
            return (false, "An error occurred.");
        }
    }

    private class CsrfResponse
    {
        public bool Success { get; set; }
        [JsonProperty("csrf_token")]
        public string? CsrfToken { get; set; }
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
    }

    private class GenericResponse
    {
        public bool Success { get; set; }
        public string? Message { get; set; }
        [JsonProperty("csrf_token")]
        public string? CsrfToken { get; set; }
    }
}