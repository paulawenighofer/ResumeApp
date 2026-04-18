using System.Text;
using System.Text.Json;

namespace ResumeApp.Services;

public class CurrentUserService
{
    public async Task<string?> GetCurrentUserIdAsync()
    {
        var token = await SecureStorage.GetAsync("auth_token");
        if (string.IsNullOrWhiteSpace(token))
        {
            return null;
        }

        try
        {
            var parts = token.Split('.');
            if (parts.Length != 3)
            {
                return null;
            }

            var payload = parts[1].Replace('-', '+').Replace('_', '/');
            payload = payload.PadRight(payload.Length + (4 - payload.Length % 4) % 4, '=');

            var json = Encoding.UTF8.GetString(Convert.FromBase64String(payload));
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            if (TryGetString(root, "nameid", out var nameId) ||
                TryGetString(root, "sub", out nameId) ||
                TryGetString(root, "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier", out nameId))
            {
                return nameId;
            }
        }
        catch
        {
        }

        return null;
    }

    private static bool TryGetString(JsonElement root, string propertyName, out string? value)
    {
        if (root.TryGetProperty(propertyName, out var property) &&
            property.ValueKind == JsonValueKind.String &&
            !string.IsNullOrWhiteSpace(property.GetString()))
        {
            value = property.GetString();
            return true;
        }

        value = null;
        return false;
    }
}
