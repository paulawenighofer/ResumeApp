using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Test.Integration.Fixtures;

namespace Test.Integration;

public class AuthAuthorizationTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;
    private readonly HttpClient _client;

    public AuthAuthorizationTests(ApiFactory factory)
    {
        _factory = factory;
        _factory.ResetDatabaseAsync().GetAwaiter().GetResult();
        _client = factory.CreateClient();
        factory.EmailService.Reset();
    }

    // ─── GET /api/auth/me ───────────────────────────────────────────────────

    [Fact]
    public async Task Me_WithoutToken_Returns401()
    {
        var res = await _client.GetAsync("api/auth/me");
        Assert.Equal(HttpStatusCode.Unauthorized, res.StatusCode);
    }

    [Fact]
    public async Task Me_WithValidToken_Returns200_WithUserInfo()
    {
        const string email = "me_ok@example.com";
        var jwt = await AuthTestHelpers.RegisterAndVerifyAsync(
            _client, _factory, email: email, firstName: "Me", lastName: "Test");

        using var authed = AuthTestHelpers.CreateAuthenticatedClient(_factory, jwt);
        var res = await authed.GetAsync("api/auth/me");

        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        var body = await res.Content.ReadFromJsonAsync<JsonElement>(AuthTestHelpers.JsonOpts);
        Assert.Equal(email, body.GetProperty("email").GetString());
        Assert.Equal("Me", body.GetProperty("firstName").GetString());
        Assert.Equal("Test", body.GetProperty("lastName").GetString());
    }

    [Fact]
    public async Task Me_WithTokenAfterLogoutAll_Returns401()
    {
        const string email = "me_revoked@example.com";
        var jwt = await AuthTestHelpers.RegisterAndVerifyAsync(_client, _factory, email: email);

        using var authed = AuthTestHelpers.CreateAuthenticatedClient(_factory, jwt);

        // Revoke all tokens via logout-all
        var logoutAllRes = await authed.PostAsync("api/auth/logout-all", null);
        Assert.Equal(HttpStatusCode.OK, logoutAllRes.StatusCode);

        // The old token should now be rejected
        var meRes = await authed.GetAsync("api/auth/me");
        Assert.Equal(HttpStatusCode.Unauthorized, meRes.StatusCode);
    }

    // ─── POST /api/auth/logout ──────────────────────────────────────────────

    [Fact]
    public async Task Logout_WithoutToken_Returns401()
    {
        var res = await _client.PostAsync("api/auth/logout", null);
        Assert.Equal(HttpStatusCode.Unauthorized, res.StatusCode);
    }

    [Fact]
    public async Task Logout_WithValidToken_Returns200()
    {
        var jwt = await AuthTestHelpers.RegisterAndVerifyAsync(
            _client, _factory, email: "logout_ok@example.com");

        using var authed = AuthTestHelpers.CreateAuthenticatedClient(_factory, jwt);
        var res = await authed.PostAsync("api/auth/logout", null);

        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
    }

    // ─── POST /api/auth/logout-all ──────────────────────────────────────────

    [Fact]
    public async Task LogoutAll_WithoutToken_Returns401()
    {
        var res = await _client.PostAsync("api/auth/logout-all", null);
        Assert.Equal(HttpStatusCode.Unauthorized, res.StatusCode);
    }

    [Fact]
    public async Task LogoutAll_WithValidToken_Returns200_AndInvalidatesPriorToken()
    {
        const string email = "logoutall_ok@example.com";
        var jwt = await AuthTestHelpers.RegisterAndVerifyAsync(_client, _factory, email: email);

        using var authed = AuthTestHelpers.CreateAuthenticatedClient(_factory, jwt);

        var res = await authed.PostAsync("api/auth/logout-all", null);
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);

        // Same token must now be rejected on any protected endpoint
        var afterRes = await authed.GetAsync("api/auth/me");
        Assert.Equal(HttpStatusCode.Unauthorized, afterRes.StatusCode);
    }
}
