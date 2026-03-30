using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Test.Integration.Fixtures;

namespace Test.Integration;

public class AuthLoginTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;
    private readonly HttpClient _client;

    public AuthLoginTests(ApiFactory factory)
    {
        _factory = factory;
        _client  = factory.CreateClient();
        factory.EmailService.Reset();
    }

    [Fact]
    public async Task Login_VerifiedUser_CorrectPassword_Returns200_WithJwt()
    {
        await AuthTestHelpers.RegisterAndVerifyAsync(_client, _factory,
            email: "login_ok@example.com", password: "Password1");

        var res = await _client.PostAsJsonAsync("api/auth/login", new
        {
            email    = "login_ok@example.com",
            password = "Password1",
        });

        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        var body = await res.Content.ReadFromJsonAsync<JsonElement>(AuthTestHelpers.JsonOpts);
        Assert.False(string.IsNullOrWhiteSpace(body.GetProperty("token").GetString()));
        Assert.Equal("login_ok@example.com", body.GetProperty("email").GetString());
    }

    [Fact]
    public async Task Login_UnverifiedUser_CorrectPassword_Returns403_WithRequiresVerification()
    {
        // Register but do NOT verify OTP
        await _client.PostAsJsonAsync("api/auth/register", new
        {
            firstName = "Unverified",
            lastName  = "User",
            email     = "unverified@example.com",
            password  = "Password1",
        });

        _factory.EmailService.Reset();

        var res = await _client.PostAsJsonAsync("api/auth/login", new
        {
            email    = "unverified@example.com",
            password = "Password1",
        });

        Assert.Equal(HttpStatusCode.Forbidden, res.StatusCode);

        var body = await res.Content.ReadFromJsonAsync<JsonElement>(AuthTestHelpers.JsonOpts);
        Assert.True(body.GetProperty("requiresVerification").GetBoolean());

        // Login should have re-sent a fresh OTP
        Assert.NotNull(_factory.EmailService.LastOtpCode);
    }

    [Fact]
    public async Task Login_WrongPassword_Returns401()
    {
        await AuthTestHelpers.RegisterAndVerifyAsync(_client, _factory,
            email: "login_badpw@example.com", password: "Password1");

        var res = await _client.PostAsJsonAsync("api/auth/login", new
        {
            email    = "login_badpw@example.com",
            password = "WrongPassword1",
        });

        Assert.Equal(HttpStatusCode.Unauthorized, res.StatusCode);
    }

    [Fact]
    public async Task Login_NonExistentEmail_Returns401()
    {
        var res = await _client.PostAsJsonAsync("api/auth/login", new
        {
            email    = "nobody@example.com",
            password = "Password1",
        });

        Assert.Equal(HttpStatusCode.Unauthorized, res.StatusCode);
    }

    [Fact]
    public async Task Login_LockedAccount_Returns423()
    {
        await AuthTestHelpers.RegisterAndVerifyAsync(_client, _factory,
            email: "lockme@example.com", password: "Password1");

        // Fire 5 bad password attempts to trigger lockout (MaxFailedAccessAttempts = 5)
        for (int i = 0; i < 5; i++)
        {
            await _client.PostAsJsonAsync("api/auth/login", new
            {
                email    = "lockme@example.com",
                password = "BadPassword1",
            });
        }

        var res = await _client.PostAsJsonAsync("api/auth/login", new
        {
            email    = "lockme@example.com",
            password = "Password1",    // correct password, but account is now locked
        });

        Assert.Equal(423, (int)res.StatusCode);
    }
}
