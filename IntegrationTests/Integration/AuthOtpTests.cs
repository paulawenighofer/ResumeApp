using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using API.Data;
using API.Services;
using Microsoft.Extensions.DependencyInjection;
using Shared.Models;
using Test.Integration.Fixtures;

namespace Test.Integration;

public class AuthOtpTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;
    private readonly HttpClient _client;

    public AuthOtpTests(ApiFactory factory)
    {
        _factory = factory;
        _factory.ResetDatabaseAsync().GetAwaiter().GetResult();
        _client = factory.CreateClient();
        factory.EmailService.Reset();
    }

    // ─── verify-otp ────────────────────────────────────────────────────────

    [Fact]
    public async Task VerifyOtp_CorrectCode_Returns200_WithJwt_AndEmailConfirmed()
    {
        const string email = "verify_ok@example.com";
        await _client.PostAsJsonAsync("api/auth/register", new
        {
            firstName = "V",
            lastName = "Ok",
            email,
            password = "Password1",
        });

        var code = _factory.EmailService.LastOtpCode!;

        var res = await _client.PostAsJsonAsync("api/auth/verify-otp", new { email, code });

        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        var body = await res.Content.ReadFromJsonAsync<JsonElement>(AuthTestHelpers.JsonOpts);
        Assert.False(string.IsNullOrWhiteSpace(body.GetProperty("token").GetString()));

        // Confirm EmailConfirmed was set in the DB
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var user = db.Users.First(u => u.Email == email);
        Assert.True(user.EmailConfirmed);
    }

    [Fact]
    public async Task VerifyOtp_WrongCode_Returns400()
    {
        const string email = "verify_wrong@example.com";
        await _client.PostAsJsonAsync("api/auth/register", new
        {
            firstName = "V",
            lastName = "Wrong",
            email,
            password = "Password1",
        });

        var res = await _client.PostAsJsonAsync("api/auth/verify-otp", new
        {
            email,
            code = "000000",   // wrong code
        });

        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
    }

    [Fact]
    public async Task VerifyOtp_ExpiredOtp_Returns400()
    {
        const string email = "verify_expired@example.com";
        await _client.PostAsJsonAsync("api/auth/register", new
        {
            firstName = "V",
            lastName = "Expired",
            email,
            password = "Password1",
        });

        var code = _factory.EmailService.LastOtpCode!;

        // Manually expire the OTP in the database
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var otp = db.OtpVerifications.First(o => o.Purpose == OtpPurpose.EmailVerification);
            otp.ExpiresAt = DateTime.UtcNow.AddMinutes(-1);
            db.SaveChanges();
        }

        var res = await _client.PostAsJsonAsync("api/auth/verify-otp", new { email, code });

        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
    }

    [Fact]
    public async Task VerifyOtp_NonExistentEmail_Returns400()
    {
        var res = await _client.PostAsJsonAsync("api/auth/verify-otp", new
        {
            email = "ghost@example.com",
            code = "123456",
        });

        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
    }

    // ─── resend-otp ─────────────────────────────────────────────────────────

    [Fact]
    public async Task ResendOtp_UnverifiedEmail_Returns200_AndSendsNewCode()
    {
        const string email = "resend_unverified@example.com";
        await _client.PostAsJsonAsync("api/auth/register", new
        {
            firstName = "R",
            lastName = "Unverified",
            email,
            password = "Password1",
        });

        var firstCode = _factory.EmailService.LastOtpCode!;
        _factory.EmailService.Reset();

        var res = await _client.PostAsJsonAsync("api/auth/resend-otp", new { email });

        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        Assert.NotNull(_factory.EmailService.LastOtpCode);
    }

    [Fact]
    public async Task ResendOtp_AlreadyVerifiedEmail_Returns200_NoCodeSent()
    {
        const string email = "resend_verified@example.com";
        await AuthTestHelpers.RegisterAndVerifyAsync(_client, _factory, email: email);
        _factory.EmailService.Reset();

        var res = await _client.PostAsJsonAsync("api/auth/resend-otp", new { email });

        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        // No OTP should have been sent for a verified user
        Assert.Null(_factory.EmailService.LastOtpCode);
    }

    [Fact]
    public async Task ResendOtp_UnknownEmail_Returns200_NoCodeSent()
    {
        // Should never disclose whether the email is registered
        var res = await _client.PostAsJsonAsync("api/auth/resend-otp", new
        {
            email = "nobody_resend@example.com",
        });

        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        Assert.Null(_factory.EmailService.LastOtpCode);
    }
}
