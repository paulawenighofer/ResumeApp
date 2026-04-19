using System.Net;
using System.Net.Http.Json;
using API.Data;
using API.Services;
using Microsoft.Extensions.DependencyInjection;
using Shared.Models;
using Test.Integration.Fixtures;

namespace Test.Integration;

public class AuthEmailDeliveryFlagTests
{
    [Fact]
    public void EmailService_FlagEnabled_RegistersSmtpEmailService()
    {
        using var factory = new ApiFactory(
            useProductionRateLimits: false,
            overrideEmailService: false,
            emailOtpDeliveryEnabled: true);
        factory.ResetDatabaseAsync().GetAwaiter().GetResult();
        using var scope = factory.Services.CreateScope();

        var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();

        Assert.IsType<SmtpEmailService>(emailService);
    }

    [Fact]
    public void EmailService_FlagDisabled_RegistersLoggingEmailService()
    {
        using var factory = new ApiFactory(
            useProductionRateLimits: false,
            overrideEmailService: false,
            emailOtpDeliveryEnabled: false);
        factory.ResetDatabaseAsync().GetAwaiter().GetResult();
        using var scope = factory.Services.CreateScope();

        var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();

        Assert.IsType<LoggingEmailService>(emailService);
    }

    [Fact]
    public async Task Register_FlagDisabled_Returns200_AndPersistsEmailVerificationOtp()
    {
        using var factory = new ApiFactory(
            useProductionRateLimits: false,
            overrideEmailService: false,
            emailOtpDeliveryEnabled: false);
        await factory.ResetDatabaseAsync();
        using var client = factory.CreateClient();

        const string email = "flag_register@example.com";
        var response = await client.PostAsJsonAsync("api/auth/register", new
        {
            firstName = "Flag",
            lastName = "Register",
            email,
            password = "Password1",
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var user = db.Users.Single(u => u.Email == email);
        var otp = db.OtpVerifications.Single(o => o.UserId == user.Id && o.Purpose == OtpPurpose.EmailVerification);

        Assert.False(user.EmailConfirmed);
        Assert.NotEmpty(otp.Code);
        Assert.True(otp.ExpiresAt > DateTime.UtcNow);
    }

    [Fact]
    public async Task ResendOtp_FlagDisabled_Returns200_AndReplacesStoredVerificationOtp()
    {
        using var factory = new ApiFactory(
            useProductionRateLimits: false,
            overrideEmailService: false,
            emailOtpDeliveryEnabled: false);
        await factory.ResetDatabaseAsync();
        using var client = factory.CreateClient();

        const string email = "flag_resend@example.com";
        await client.PostAsJsonAsync("api/auth/register", new
        {
            firstName = "Flag",
            lastName = "Resend",
            email,
            password = "Password1",
        });

        int firstOtpId;
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var user = db.Users.Single(u => u.Email == email);
            firstOtpId = db.OtpVerifications
                .Single(o => o.UserId == user.Id && o.Purpose == OtpPurpose.EmailVerification)
                .Id;
        }

        var response = await client.PostAsJsonAsync("api/auth/resend-otp", new { email });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var user = db.Users.Single(u => u.Email == email);
            var verificationOtps = db.OtpVerifications
                .Where(o => o.UserId == user.Id && o.Purpose == OtpPurpose.EmailVerification)
                .ToList();

            Assert.Single(verificationOtps);
            Assert.NotEqual(firstOtpId, verificationOtps[0].Id);
        }
    }

    [Fact]
    public async Task ForgotPassword_FlagDisabled_Returns200_AndPersistsPasswordResetOtp()
    {
        using var factory = new ApiFactory(
            useProductionRateLimits: false,
            overrideEmailService: false,
            emailOtpDeliveryEnabled: false);
        await factory.ResetDatabaseAsync();
        using var client = factory.CreateClient();

        const string email = "flag_forgot@example.com";
        await client.PostAsJsonAsync("api/auth/register", new
        {
            firstName = "Flag",
            lastName = "Forgot",
            email,
            password = "Password1",
        });

        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var user = db.Users.Single(u => u.Email == email);
            user.EmailConfirmed = true;
            db.SaveChanges();
        }

        var response = await client.PostAsJsonAsync("api/auth/forgot-password", new { email });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var verifyScope = factory.Services.CreateScope();
        var verifyDb = verifyScope.ServiceProvider.GetRequiredService<AppDbContext>();
        var verifiedUser = verifyDb.Users.Single(u => u.Email == email);
        var resetOtp = verifyDb.OtpVerifications.Single(o => o.UserId == verifiedUser.Id && o.Purpose == OtpPurpose.PasswordReset);

        Assert.NotEmpty(resetOtp.Code);
        Assert.True(resetOtp.ExpiresAt > DateTime.UtcNow);
    }
}
