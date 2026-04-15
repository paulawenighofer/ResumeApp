using CommunityToolkit.Datasync.Client.Authentication;

namespace ResumeApp.Services;

public sealed class DatasyncAuthProvider : GenericAuthenticationProvider
{
    public DatasyncAuthProvider() : base(GetTokenAsync)
    {
        RefreshBufferTimeSpan = TimeSpan.FromSeconds(30);
    }

    private static async Task<AuthenticationToken> GetTokenAsync(CancellationToken cancellationToken = default)
    {
        var token = await SecureStorage.GetAsync("auth_token") ?? string.Empty;
        var userId = await SecureStorage.GetAsync("user_id") ?? string.Empty;
        var displayName = await SecureStorage.GetAsync("user_name") ?? string.Empty;

        return new AuthenticationToken
        {
            Token = token,
            UserId = userId,
            DisplayName = displayName,
            ExpiresOn = DateTimeOffset.UtcNow.AddMinutes(30)
        };
    }
}
