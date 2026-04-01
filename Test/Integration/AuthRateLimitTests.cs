using System.Net;
using System.Net.Http.Json;
using Test.Integration.Fixtures;

namespace Test.Integration;

/// <summary>
/// Rate-limit tests use a fresh ApiFactory per test class to ensure the sliding-window
/// counters start at zero.  Do NOT use IClassFixture here.
/// </summary>
public class AuthRateLimitTests : IDisposable
{
    private readonly ApiFactory _factory;
    private readonly HttpClient _client;

    public AuthRateLimitTests()
    {
        _factory = new ApiFactory(useProductionRateLimits: true);
        _client  = _factory.CreateClient();
    }

    public void Dispose()
    {
        _client.Dispose();
        _factory.Dispose();
    }

    // ─── otp-verify (5 permits / 15 min) applies to verify-otp ────────────

    [Fact]
    public async Task VerifyOtp_ExceedsRateLimit_Returns429_OnSixthRequest()
    {
        const string email = "rl_verifyotp@example.com";

        // Register so there is a valid user (wrong codes still count against the limit)
        await _client.PostAsJsonAsync("api/auth/register", new
        {
            firstName = "RL", lastName = "User", email, password = "Password1",
        });

        for (int i = 0; i < 5; i++)
        {
            await _client.PostAsJsonAsync("api/auth/verify-otp", new
            {
                email,
                code = "000000",
            });
        }

        var res = await _client.PostAsJsonAsync("api/auth/verify-otp", new
        {
            email,
            code = "000000",
        });

        Assert.Equal(429, (int)res.StatusCode);
    }

    // ─── otp-verify (5 permits / 15 min) applies to reset-password ─────────

    [Fact]
    public async Task ResetPassword_ExceedsRateLimit_Returns429_OnSixthRequest()
    {
        const string email = "rl_resetpw@example.com";
        await AuthTestHelpers.RegisterAndVerifyAsync(_client, _factory, email: email);
        await _client.PostAsJsonAsync("api/auth/forgot-password", new { email });

        for (int i = 0; i < 5; i++)
        {
            await _client.PostAsJsonAsync("api/auth/reset-password", new
            {
                email,
                code        = "000000",
                newPassword = "NewPassword1",
            });
        }

        var res = await _client.PostAsJsonAsync("api/auth/reset-password", new
        {
            email,
            code        = "000000",
            newPassword = "NewPassword1",
        });

        Assert.Equal(429, (int)res.StatusCode);
    }

    // ─── otp-send (3 permits / 10 min) applies to forgot-password ──────────

    [Fact]
    public async Task ForgotPassword_ExceedsRateLimit_Returns429_OnFourthRequest()
    {
        const string email = "rl_forgotpw@example.com";
        await AuthTestHelpers.RegisterAndVerifyAsync(_client, _factory, email: email);

        for (int i = 0; i < 3; i++)
        {
            await _client.PostAsJsonAsync("api/auth/forgot-password", new { email });
        }

        var res = await _client.PostAsJsonAsync("api/auth/forgot-password", new { email });

        Assert.Equal(429, (int)res.StatusCode);
    }

    // ─── otp-send (3 permits / 10 min) applies to resend-otp ───────────────

    [Fact]
    public async Task ResendOtp_ExceedsRateLimit_Returns429_OnFourthRequest()
    {
        const string email = "rl_resendotp@example.com";
        await _client.PostAsJsonAsync("api/auth/register", new
        {
            firstName = "RL", lastName = "Resend", email, password = "Password1",
        });

        for (int i = 0; i < 3; i++)
        {
            await _client.PostAsJsonAsync("api/auth/resend-otp", new { email });
        }

        var res = await _client.PostAsJsonAsync("api/auth/resend-otp", new { email });

        Assert.Equal(429, (int)res.StatusCode);
    }
}
