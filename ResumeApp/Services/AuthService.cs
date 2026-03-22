using Shared.DTO;
using System.Net.Http.Headers;
using System.Net.Http.Json;

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

    public async Task<bool> LoginAsync(string email, string password)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("api/auth/login",
                new
                {
                    email,
                    password
                });

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

    public async Task<bool> RegisterAsync(
    string firstName, string lastName, string email, string password)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("api/auth/register",
                new
                {
                    firstName,
                    lastName,
                    email,
                    password
                });

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