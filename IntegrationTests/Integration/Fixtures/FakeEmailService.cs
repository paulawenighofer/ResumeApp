using API.Services;

namespace Test.Integration.Fixtures;

/// <summary>
/// Test double for IEmailService that captures sent codes instead of using SMTP.
/// Tests read LastOtpCode / LastResetCode to retrieve the OTP without needing a real inbox.
/// </summary>
public class FakeEmailService : IEmailService
{
    public string? LastOtpEmail { get; private set; }
    public string? LastOtpCode { get; private set; }
    public string? LastResetEmail { get; private set; }
    public string? LastResetCode { get; private set; }

    public bool ShouldThrow { get; set; }

    public Task SendOtpAsync(string toEmail, string code)
    {
        if (ShouldThrow) throw new InvalidOperationException("Simulated email failure.");
        LastOtpEmail = toEmail;
        LastOtpCode = code;
        return Task.CompletedTask;
    }

    public Task SendPasswordResetOtpAsync(string toEmail, string code)
    {
        if (ShouldThrow) throw new InvalidOperationException("Simulated email failure.");
        LastResetEmail = toEmail;
        LastResetCode = code;
        return Task.CompletedTask;
    }

    public void Reset()
    {
        LastOtpEmail = null;
        LastOtpCode = null;
        LastResetEmail = null;
        LastResetCode = null;
        ShouldThrow = false;
    }
}
