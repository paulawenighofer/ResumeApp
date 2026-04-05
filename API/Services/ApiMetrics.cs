using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace API.Services;

public sealed class ApiMetrics : IDisposable
{
    public const string MeterName = "ResumeApp.API";
    private const string MeterVersion = "2.0.0";

    private readonly Meter _meter;
    private readonly UserActivityTracker _activityTracker;
    private readonly string _uploadsRoot;

    private readonly Counter<long> _registrations;
    private readonly Counter<long> _httpRequests;
    private readonly Counter<long> _loginAttempts;
    private readonly Counter<long> _otpVerifications;
    private readonly Counter<long> _passwordResetRequests;
    private readonly Counter<long> _socialLogins;
    private readonly Counter<long> _emailSends;
    private readonly Histogram<double> _emailSendDurationMs;
    private readonly Counter<long> _profileMutations;
    private readonly Counter<long> _uploadOperations;
    private readonly Histogram<long> _uploadBytes;
    private readonly Histogram<double> _uploadDurationMs;

    public ApiMetrics(UserActivityTracker activityTracker, IWebHostEnvironment environment)
    {
        _activityTracker = activityTracker;
        _uploadsRoot = ResolveUploadsRoot(environment);

        _meter = new Meter(MeterName, MeterVersion);

        _registrations = _meter.CreateCounter<long>(
            "resumeapp_auth_registrations_total",
            description: "Count of user registration attempts by outcome");

        _httpRequests = _meter.CreateCounter<long>(
            "resumeapp_http_requests_total",
            description: "Count of completed API HTTP requests by scheme, method, route, and status code");

        _loginAttempts = _meter.CreateCounter<long>(
            "resumeapp_auth_login_attempts_total",
            description: "Count of login attempts by outcome");

        _otpVerifications = _meter.CreateCounter<long>(
            "resumeapp_auth_otp_verifications_total",
            description: "Count of OTP verification attempts by outcome");

        _passwordResetRequests = _meter.CreateCounter<long>(
            "resumeapp_auth_password_reset_requests_total",
            description: "Count of password reset requests");

        _socialLogins = _meter.CreateCounter<long>(
            "resumeapp_auth_social_logins_total",
            description: "Count of social login callbacks by provider and outcome");

        _emailSends = _meter.CreateCounter<long>(
            "resumeapp_email_sends_total",
            description: "Count of transactional email sends by template and outcome");

        _emailSendDurationMs = _meter.CreateHistogram<double>(
            "resumeapp_email_send_duration_ms",
            unit: "ms",
            description: "Time to send transactional emails");

        _profileMutations = _meter.CreateCounter<long>(
            "resumeapp_profile_mutations_total",
            description: "Count of profile data mutations by section and action");

        _uploadOperations = _meter.CreateCounter<long>(
            "resumeapp_upload_operations_total",
            description: "Count of upload operations by category and outcome");

        _uploadBytes = _meter.CreateHistogram<long>(
            "resumeapp_upload_bytes",
            unit: "By",
            description: "Upload size for profile and project assets");

        _uploadDurationMs = _meter.CreateHistogram<double>(
            "resumeapp_upload_duration_ms",
            unit: "ms",
            description: "Duration of upload operations");

        _meter.CreateObservableGauge<long>(
            "resumeapp_active_users",
            ObserveActiveUsers,
            unit: "{users}",
            description: "Distinct authenticated users active in the last five minutes");

        _meter.CreateObservableGauge<long>(
            "resumeapp_storage_upload_bytes",
            ObserveUploadStorageBytes,
            unit: "By",
            description: "Total bytes stored in the uploads directory");
    }

    public void RecordRegistration(string outcome) =>
        _registrations.Add(1, CreateTags(("outcome", outcome)));

    public void RecordHttpRequest(string scheme, string method, string route, int statusCode) =>
        _httpRequests.Add(1, CreateTags(
            ("scheme", scheme),
            ("method", method),
            ("route", route),
            ("status_code", statusCode.ToString())));

    public void RecordLoginAttempt(string outcome) =>
        _loginAttempts.Add(1, CreateTags(("outcome", outcome)));

    public void RecordOtpVerification(string outcome) =>
        _otpVerifications.Add(1, CreateTags(("outcome", outcome)));

    public void RecordPasswordResetRequested() =>
        _passwordResetRequests.Add(1);

    public void RecordSocialLogin(string provider, string outcome) =>
        _socialLogins.Add(1, CreateTags(("provider", provider), ("outcome", outcome)));

    public void RecordEmailSend(string template, string outcome, double durationMs)
    {
        var tags = CreateTags(("template", template), ("outcome", outcome));
        _emailSends.Add(1, tags);
        _emailSendDurationMs.Record(durationMs, tags);
    }

    public void RecordProfileMutation(string section, string action, string? userId = null)
    {
        _profileMutations.Add(1, CreateTags(("section", section), ("action", action)));

        if (!string.IsNullOrWhiteSpace(userId))
        {
            _activityTracker.RecordActivity(userId);
        }
    }

    public void RecordUpload(string category, string outcome, int fileCount, long totalBytes, double durationMs, string? userId = null)
    {
        var tags = CreateTags(("category", category), ("outcome", outcome));
        _uploadOperations.Add(1, tags);
        _uploadBytes.Record(totalBytes, tags);
        _uploadDurationMs.Record(durationMs, tags);

        if (!string.IsNullOrWhiteSpace(userId))
        {
            _activityTracker.RecordActivity(userId);
        }
    }

    public void RecordUserActivity(string? userId)
    {
        if (!string.IsNullOrWhiteSpace(userId))
        {
            _activityTracker.RecordActivity(userId);
        }
    }

    public void Dispose() => _meter.Dispose();

    private static TagList CreateTags(params (string Key, object? Value)[] values)
    {
        var tags = new TagList();
        foreach (var (key, value) in values)
        {
            tags.Add(key, value);
        }

        return tags;
    }

    private Measurement<long> ObserveActiveUsers() => new(_activityTracker.GetActiveUserCount());

    private Measurement<long> ObserveUploadStorageBytes() => new(MeasureDirectoryBytes(_uploadsRoot));

    private static string ResolveUploadsRoot(IWebHostEnvironment environment)
    {
        var webRoot = string.IsNullOrWhiteSpace(environment.WebRootPath)
            ? Path.Combine(environment.ContentRootPath, "wwwroot")
            : environment.WebRootPath;

        return Path.Combine(webRoot, "uploads");
    }

    private static long MeasureDirectoryBytes(string rootPath)
    {
        try
        {
            if (!Directory.Exists(rootPath))
            {
                return 0;
            }

            return Directory.EnumerateFiles(rootPath, "*", SearchOption.AllDirectories)
                .Select(path => new FileInfo(path).Length)
                .Sum();
        }
        catch
        {
            return 0;
        }
    }
}
