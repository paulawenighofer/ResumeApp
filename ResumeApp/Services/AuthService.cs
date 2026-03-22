using Shared.DTO;
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
        return !string.IsNullOrEmpty(token);
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

    public void Logout()
    {
        SecureStorage.Remove("auth_token");
        SecureStorage.Remove("user_name");
    }

    private static async Task SaveUserNameAsync(string? firstName, string? lastName, string? email)
    {
        var fullName = $"{firstName} {lastName}".Trim();
        var nameToSave = !string.IsNullOrWhiteSpace(fullName) ? fullName : (email ?? "User");
        await SecureStorage.SetAsync("user_name", nameToSave);
    }
}