using Shared.DTO;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace ResumeApp.Services;

public class AuthService
{
    private readonly HttpClient _httpClient;
    public string BaseUrl
    {
        get;
    }

    public AuthService(HttpClient httpClient)
    {
        _httpClient = httpClient;
        BaseUrl = _httpClient.BaseAddress?.ToString().TrimEnd('/') ?? "";
    }

    public record LoginResult(bool Success, bool RequiresVerification = false, string? Email = null);
    public record RegisterResult(bool Success, string? Email = null, string? ErrorMessage = null);

    public async Task<LoginResult> LoginAsync(string email, string password)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("api/auth/login",
                new { email, password });

            // 403 means the user exists and password is correct, but email isn't verified yet
            if ((int)response.StatusCode == 403)
            {
                var body = await response.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>();
                var returnedEmail = body.TryGetProperty("email", out var e) ? e.GetString() : email;
                return new LoginResult(false, RequiresVerification: true, Email: returnedEmail);
            }

            if (!response.IsSuccessStatusCode)
                return new LoginResult(false);

            var result = await response.Content.ReadFromJsonAsync<AuthResponseDto>();

            if (result?.Token != null)
            {
                await SecureStorage.SetAsync("auth_token", result.Token);
                await SecureStorage.SetAsync("user_email", result.Email);
                await SaveUserNameAsync(result.FirstName, result.LastName, result.Email);
                return new LoginResult(true);
            }

            return new LoginResult(false);
        }
        catch
        {
            return new LoginResult(false);
        }
    }

    public async Task<bool> IsLoggedInAsync()
    {
        var token = await SecureStorage.GetAsync("auth_token");
        if (string.IsNullOrEmpty(token)) return false;

        // Decode the JWT expiry claim without any external library.
        // JWTs are just three base64url-encoded JSON segments separated by dots.
        // The middle segment (payload) contains the "exp" claim — a Unix timestamp.
        var expiry = GetTokenExpiry(token);
        if (expiry == null || expiry <= DateTime.UtcNow)
        {
            // Token is expired or unreadable — clear it so OnStart redirects to login
            ClearLocalSession();
            return false;
        }

        return true;
    }

    private static DateTime? GetTokenExpiry(string token)
    {
        try
        {
            var parts = token.Split('.');
            if (parts.Length != 3) return null;

            // Base64url → Base64: replace url-safe chars and add padding
            var payload = parts[1].Replace('-', '+').Replace('_', '/');
            payload = payload.PadRight(payload.Length + (4 - payload.Length % 4) % 4, '=');

            var json = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(payload));
            using var doc = System.Text.Json.JsonDocument.Parse(json);

            if (doc.RootElement.TryGetProperty("exp", out var exp))
                return DateTimeOffset.FromUnixTimeSeconds(exp.GetInt64()).UtcDateTime;

            return null;
        }
        catch { return null; }
    }

    // Returns success + email (for OTP navigation) or a friendly error message.
    public async Task<RegisterResult> RegisterAsync(
        string firstName, string lastName, string email, string password)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("api/auth/register",
                new { firstName, lastName, email, password });

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<RegisterPendingResponseDto>();
                if (!string.IsNullOrWhiteSpace(result?.Email))
                    return new RegisterResult(true, result.Email);

                return new RegisterResult(false, ErrorMessage: "Registration failed. Please try again.");
            }

            var error = await ExtractApiErrorMessageAsync(response, "Registration failed. Please try again.");
            return new RegisterResult(false, ErrorMessage: error);
        }
        catch (HttpRequestException)
        {
            return new RegisterResult(false, ErrorMessage: "Unable to connect to server.");
        }
        catch (TaskCanceledException)
        {
            return new RegisterResult(false, ErrorMessage: "Request timed out. Please try again.");
        }
        catch
        {
            return new RegisterResult(false, ErrorMessage: "Something went wrong. Please try again.");
        }
    }

    private static async Task<string> ExtractApiErrorMessageAsync(HttpResponseMessage response, string fallback)
    {
        var bodyText = await response.Content.ReadAsStringAsync();
        if (string.IsNullOrWhiteSpace(bodyText))
            return fallback;

        try
        {
            using var doc = JsonDocument.Parse(bodyText);
            var root = doc.RootElement;

            if (root.TryGetProperty("message", out var message) && !string.IsNullOrWhiteSpace(message.GetString()))
                return message.GetString()!;

            // Identity validation failures come back as { "errors": ["msg1", "msg2"] }
            if (root.TryGetProperty("errors", out var errors) && errors.ValueKind == JsonValueKind.Array)
            {
                var messages = errors.EnumerateArray()
                    .Select(e => e.GetString())
                    .Where(s => !string.IsNullOrWhiteSpace(s));
                var joined = string.Join(" ", messages);
                if (!string.IsNullOrWhiteSpace(joined))
                    return joined;
            }
        }
        catch
        {
            // Not JSON or unexpected shape; return raw API text below.
        }

        return bodyText.Trim();
    }

    // Submits the 6-digit OTP code. On success, stores the JWT and user info.
    public async Task<bool> VerifyOtpAsync(string email, string code)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("api/auth/verify-otp",
                new { email, code });

            if (!response.IsSuccessStatusCode)
                return false;

            var result = await response.Content.ReadFromJsonAsync<AuthResponseDto>();

            if (result?.Token != null)
            {
                await SecureStorage.SetAsync("auth_token", result.Token);
                await SecureStorage.SetAsync("user_email", result.Email);
                await SaveUserNameAsync(result.FirstName, result.LastName, result.Email);
                return true;
            }

            return false;
        }
        catch
        {
            return false;
        }
    }

    // Requests a fresh OTP for an unverified email.
    public async Task<bool> ResendOtpAsync(string email)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("api/auth/resend-otp",
                new { email });
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    public record ForgotPasswordResult(
        bool Success,
        bool RequiresVerification = false,
        string? Email = null,
        string? Message = null);

    // Sends a password reset email.
    // Returns RequiresVerification=true if the account exists but OTP hasn't been completed.
    public async Task<ForgotPasswordResult> ForgotPasswordAsync(string email)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("api/auth/forgot-password",
                new { email });

            if (response.IsSuccessStatusCode)
                return new ForgotPasswordResult(true);

            // 400 with requiresVerification means the account exists but isn't verified
            if ((int)response.StatusCode == 400)
            {
                var body = await response.Content.ReadFromJsonAsync<JsonElement>();
                if (body.TryGetProperty("requiresVerification", out var flag) && flag.GetBoolean())
                {
                    var returnedEmail = body.TryGetProperty("email", out var e) ? e.GetString() : email;
                    var message = body.TryGetProperty("message", out var m) ? m.GetString() : null;
                    return new ForgotPasswordResult(false, RequiresVerification: true, Email: returnedEmail, Message: message);
                }
            }

            return new ForgotPasswordResult(false, Message: "Something went wrong. Please try again.");
        }
        catch
        {
            return new ForgotPasswordResult(false, Message: "Unable to connect. Please try again.");
        }
    }

    // Submits the 6-digit OTP code and new password to complete password reset.
    public async Task<(bool Success, string? Error)> ResetPasswordAsync(
        string email, string code, string newPassword)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("api/auth/reset-password",
                new { email, code, newPassword });

            if (response.IsSuccessStatusCode)
                return (true, null);

            var error = await ExtractApiErrorMessageAsync(response, "Password reset failed. Please try again.");
            return (false, error);
        }
        catch
        {
            return (false, "Something went wrong. Please try again.");
        }
    }

    /// <summary>
    /// Calls GET /api/auth/me and persists the user's name and email to SecureStorage.
    /// Called after social login, where the MAUI app only receives a bare JWT token
    /// with no accompanying profile payload.
    /// </summary>
    public async Task FetchAndSaveUserInfoAsync()
    {
        try
        {
            var request = await BuildAuthorizedRequestAsync(HttpMethod.Get, "api/auth/me");
            var response = await _httpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode) return;

            var result = await response.Content.ReadFromJsonAsync<AuthResponseDto>();
            if (result == null) return;

            await SecureStorage.SetAsync("user_email", result.Email);
            await SaveUserNameAsync(result.FirstName, result.LastName, result.Email);
        }
        catch { }
    }

    /// <summary>
    /// Logs out from the current device only.
    /// Notifies the server (for audit purposes) then clears local SecureStorage.
    /// </summary>
    public async Task LogoutAsync()
    {
        try
        {
            var request = await BuildAuthorizedRequestAsync(HttpMethod.Post, "api/auth/logout");
            await _httpClient.SendAsync(request);
        }
        catch { }
        finally
        {
            ClearLocalSession();
        }
    }

    /// <summary>
    /// Logs out from all devices by telling the server to rotate the SecurityStamp.
    /// Every token issued before this call will be rejected by the server immediately,
    /// regardless of expiry. Then clears the local session too.
    /// </summary>
    public async Task LogoutAllDevicesAsync()
    {
        try
        {
            var request = await BuildAuthorizedRequestAsync(HttpMethod.Post, "api/auth/logout-all");
            await _httpClient.SendAsync(request);
        }
        catch { }
        finally
        {
            ClearLocalSession();
        }
    }

    private void ClearLocalSession()
    {
        SecureStorage.Remove("auth_token");
        SecureStorage.Remove("user_name");
        SecureStorage.Remove("user_email");
    }

    private async Task<HttpRequestMessage> BuildAuthorizedRequestAsync(HttpMethod method, string url)
    {
        var token = await SecureStorage.GetAsync("auth_token");
        var request = new HttpRequestMessage(method, url);
        if (token != null)
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return request;
    }

    private static async Task SaveUserNameAsync(string? firstName, string? lastName, string? email)
    {
        var fullName = $"{firstName} {lastName}".Trim();
        var nameToSave = !string.IsNullOrWhiteSpace(fullName) ? fullName : (email ?? "User");
        await SecureStorage.SetAsync("user_name", nameToSave);
    }
}