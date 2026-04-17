namespace API.Services;

public class LoggingEmailService : IEmailService
{
    private readonly ApiMetrics _metrics;
    private readonly ILogger<LoggingEmailService> _logger;

    public LoggingEmailService(ApiMetrics metrics, ILogger<LoggingEmailService> logger)
    {
        _metrics = metrics;
        _logger = logger;
    }

    public Task SendOtpAsync(string toEmail, string code)
    {
        LogDisabledDelivery(toEmail, code, TelemetryTags.EmailTemplates.Verification);
        return Task.CompletedTask;
    }

    public Task SendPasswordResetOtpAsync(string toEmail, string code)
    {
        LogDisabledDelivery(toEmail, code, TelemetryTags.EmailTemplates.PasswordReset);
        return Task.CompletedTask;
    }

    private void LogDisabledDelivery(string toEmail, string code, string template)
    {
        _logger.LogWarning(
            "OTP email delivery disabled by feature flag. Template {Template} for {Recipient} was not sent. Staging code: {OtpCode}",
            template,
            toEmail,
            code);

        _metrics.RecordEmailSend(template, TelemetryTags.Outcomes.Disabled, 0);
    }
}
