using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using API.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Shared.Models;
using Test.Integration.Fixtures;

namespace Test.Integration;

public class AuthForgotResetTests : IDisposable
{
    private readonly ApiFactory _factory;
    private readonly HttpClient _client;

    public AuthForgotResetTests()
    {
        _factory = new ApiFactory();
        _client = _factory.CreateClient();
        _factory.EmailService.Reset();
    }

    public void Dispose()
    {
        _client.Dispose();
        _factory.Dispose();
    }

    // ─── forgot-password ────────────────────────────────────────────────────

    [Fact]
    public async Task ForgotPassword_VerifiedUser_Returns200_AndSendsResetCode()
    {
        const string email = "fp_verified@example.com";
        await AuthTestHelpers.RegisterAndVerifyAsync(_client, _factory, email: email);
        _factory.EmailService.Reset();

        var res = await _client.PostAsJsonAsync("api/auth/forgot-password", new { email });

        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        Assert.Equal(email, _factory.EmailService.LastResetEmail);
        Assert.NotNull(_factory.EmailService.LastResetCode);
        Assert.Equal(6, _factory.EmailService.LastResetCode!.Length);
    }

    [Fact]
    public async Task ForgotPassword_UnknownEmail_Returns200_NoCodeSent()
    {
        // Must not reveal whether the email is registered
        var res = await _client.PostAsJsonAsync("api/auth/forgot-password", new
        {
            email = "unknown_fp@example.com",
        });

        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        Assert.Null(_factory.EmailService.LastResetCode);
    }

    [Fact]
    public async Task ForgotPassword_UnverifiedUser_Returns400_WithRequiresVerification()
    {
        // Register but do NOT verify
        const string email = "fp_unverified@example.com";
        await _client.PostAsJsonAsync("api/auth/register", new
        {
            firstName = "FP",
            lastName = "Unverified",
            email,
            password = "Password1",
        });
        _factory.EmailService.Reset();

        var res = await _client.PostAsJsonAsync("api/auth/forgot-password", new { email });

        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
        var body = await res.Content.ReadFromJsonAsync<JsonElement>(AuthTestHelpers.JsonOpts);
        Assert.True(body.GetProperty("requiresVerification").GetBoolean());
    }

    [Fact]
    public async Task ForgotPassword_InvalidEmail_Returns400()
    {
        var res = await _client.PostAsJsonAsync("api/auth/forgot-password", new
        {
            email = "not-an-email",
        });

        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
    }

    // ─── reset-password ─────────────────────────────────────────────────────

    [Fact]
    public async Task ResetPassword_CorrectOtp_ValidPassword_Returns200()
    {
        const string email = "rp_ok@example.com";
        await AuthTestHelpers.RegisterAndVerifyAsync(_client, _factory, email: email);
        _factory.EmailService.Reset();

        await _client.PostAsJsonAsync("api/auth/forgot-password", new { email });
        var code = _factory.EmailService.LastResetCode!;

        var res = await _client.PostAsJsonAsync("api/auth/reset-password", new
        {
            email,
            code,
            newPassword = "NewPassword1",
        });

        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
    }

    [Fact]
    public async Task ResetPassword_WrongOtp_Returns400()
    {
        const string email = "rp_wrong@example.com";
        await AuthTestHelpers.RegisterAndVerifyAsync(_client, _factory, email: email);
        await _client.PostAsJsonAsync("api/auth/forgot-password", new { email });

        var res = await _client.PostAsJsonAsync("api/auth/reset-password", new
        {
            email,
            code = "000000",
            newPassword = "NewPassword1",
        });

        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
    }

    [Fact]
    public async Task ResetPassword_ExpiredOtp_Returns400()
    {
        const string email = "rp_expired@example.com";
        await AuthTestHelpers.RegisterAndVerifyAsync(_client, _factory, email: email);
        await _client.PostAsJsonAsync("api/auth/forgot-password", new { email });
        var code = _factory.EmailService.LastResetCode!;

        // Manually expire the OTP for this specific user
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var userId = await db.Users
                .Where(u => u.Email == email)
                .Select(u => u.Id)
                .SingleAsync();

            var otp = await db.OtpVerifications
                .Where(o => o.UserId == userId && o.Purpose == OtpPurpose.PasswordReset)
                .OrderByDescending(o => o.CreatedAt)
                .FirstAsync();

            otp.ExpiresAt = DateTime.UtcNow.AddMinutes(-1);
            await db.SaveChangesAsync();
        }

        var res = await _client.PostAsJsonAsync("api/auth/reset-password", new
        {
            email,
            code,
            newPassword = "NewPassword1",
        });

        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
    }

    [Fact]
    public async Task ResetPassword_WeakPassword_Returns400()
    {
        const string email = "rp_weak@example.com";
        await AuthTestHelpers.RegisterAndVerifyAsync(_client, _factory, email: email);
        await _client.PostAsJsonAsync("api/auth/forgot-password", new { email });
        var code = _factory.EmailService.LastResetCode!;

        var res = await _client.PostAsJsonAsync("api/auth/reset-password", new
        {
            email,
            code,
            newPassword = "weak",   // fails Identity password rules
        });

        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
    }

    [Fact]
    public async Task ResetPassword_OtpUsedTwice_Returns400()
    {
        const string email = "rp_singleuse@example.com";
        await AuthTestHelpers.RegisterAndVerifyAsync(_client, _factory, email: email);
        await _client.PostAsJsonAsync("api/auth/forgot-password", new { email });
        var code = _factory.EmailService.LastResetCode!;

        // First use — should succeed
        await _client.PostAsJsonAsync("api/auth/reset-password", new
        {
            email,
            code,
            newPassword = "NewPassword1",
        });

        // Second use — OTP was deleted, should fail
        var res = await _client.PostAsJsonAsync("api/auth/reset-password", new
        {
            email,
            code,
            newPassword = "AnotherPassword1",
        });

        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
    }

    [Fact]
    public async Task ResetPassword_CanLoginWithNewPassword_AfterReset()
    {
        const string email = "rp_e2e@example.com";
        const string oldPassword = "OldPassword1";
        const string newPassword = "NewPassword1";

        await AuthTestHelpers.RegisterAndVerifyAsync(_client, _factory,
            email: email, password: oldPassword);
        _factory.EmailService.Reset();

        await _client.PostAsJsonAsync("api/auth/forgot-password", new { email });
        var code = _factory.EmailService.LastResetCode!;

        await _client.PostAsJsonAsync("api/auth/reset-password", new
        {
            email,
            code,
            newPassword,
        });

        // Should be able to log in with the NEW password
        var loginRes = await _client.PostAsJsonAsync("api/auth/login", new
        {
            email,
            password = newPassword,
        });
        Assert.Equal(HttpStatusCode.OK, loginRes.StatusCode);

        // Old password should no longer work
        var oldLoginRes = await _client.PostAsJsonAsync("api/auth/login", new
        {
            email,
            password = oldPassword,
        });
        Assert.Equal(HttpStatusCode.Unauthorized, oldLoginRes.StatusCode);
    }
}
