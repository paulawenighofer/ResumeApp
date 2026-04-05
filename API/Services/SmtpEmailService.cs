using System.Net;
using System.Net.Mail;
using System.Diagnostics;

namespace API.Services
{
    public class SmtpEmailService : IEmailService
    {
        private readonly IConfiguration _config;
        private readonly ApiMetrics _metrics;
        private readonly ILogger<SmtpEmailService> _logger;

        public SmtpEmailService(IConfiguration config, ApiMetrics metrics, ILogger<SmtpEmailService> logger)
        {
            _config = config;
            _metrics = metrics;
            _logger = logger;
        }

        public async Task SendOtpAsync(string toEmail, string code)
        {
            var subject = "Your verification code";
            var body = $@"
                <p>Hi,</p>
                <p>Your verification code is:</p>
                <h2 style=""letter-spacing: 8px; font-size: 36px;"">{code}</h2>
                <p>This code expires in <strong>10 minutes</strong>. Do not share it with anyone.</p>
                <p>If you didn't request this, you can ignore this email.</p>
            ";

            await SendAsync(toEmail, subject, body, TelemetryTags.EmailTemplates.Verification);
        }

        public async Task SendPasswordResetOtpAsync(string toEmail, string code)
        {
            var subject = "Your password reset code";
            var body = $@"
                <p>Hi,</p>
                <p>We received a request to reset your password. Enter this code in the app to continue:</p>
                <h2 style=""letter-spacing: 8px; font-size: 36px;"">{code}</h2>
                <p>This code expires in <strong>10 minutes</strong>. Do not share it with anyone.</p>
                <p>If you didn't request a password reset, you can ignore this email.</p>
            ";

            await SendAsync(toEmail, subject, body, TelemetryTags.EmailTemplates.PasswordReset);
        }

        private async Task SendAsync(string toEmail, string subject, string htmlBody, string template)
        {
            var stopwatch = Stopwatch.StartNew();

            try
            {
                var host = _config["Smtp:Host"]!;
                var port = int.Parse(_config["Smtp:Port"]!);
                var username = _config["Smtp:Username"]!;
                var password = _config["Smtp:Password"]!;
                var from = _config["Smtp:From"]!;
                var senderName = _config["Smtp:SenderName"];

                var enableSsl = bool.Parse(_config["Smtp:EnableSsl"] ?? "true");

                using var client = new SmtpClient(host, port)
                {
                    Credentials = new NetworkCredential(username, password),
                    EnableSsl = enableSsl
                };

                using var message = new MailMessage
                {
                    Subject = subject,
                    Body = htmlBody,
                    IsBodyHtml = true,
                    From = string.IsNullOrWhiteSpace(senderName)
                        ? new MailAddress(from)
                        : new MailAddress(from, senderName)
                };

                message.To.Add(toEmail);
                await client.SendMailAsync(message);
                stopwatch.Stop();
                _metrics.RecordEmailSend(template, TelemetryTags.Outcomes.Success, stopwatch.Elapsed.TotalMilliseconds);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(
                    ex,
                    "Transactional email send failed for template {Template} to {Recipient}",
                    template,
                    toEmail);
                _metrics.RecordEmailSend(template, TelemetryTags.Outcomes.Failure, stopwatch.Elapsed.TotalMilliseconds);
                throw;
            }
        }
    }
}
