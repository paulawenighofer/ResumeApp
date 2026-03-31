using System.Diagnostics.Metrics;

namespace API.Services;

public class ApiMetrics : IDisposable
{
    public const string MeterName = "ResumeApp.API";

    private readonly Meter _meter;

    // Auth metrics
    public Counter<long> UserRegistrations { get; }
    public Counter<long> LoginAttempts { get; }
    public Counter<long> OtpVerifications { get; }
    public Counter<long> PasswordResets { get; }
    public Counter<long> SocialLogins { get; }

    // Resume data metrics
    public Counter<long> EducationsCreated { get; }
    public Counter<long> ExperiencesCreated { get; }
    public Counter<long> SkillsCreated { get; }
    public Counter<long> ProjectsCreated { get; }

    public ApiMetrics()
    {
        _meter = new Meter(MeterName, "1.0.0");

        UserRegistrations = _meter.CreateCounter<long>(
            "resumeapp.auth.registrations",
            description: "Number of user registration attempts",
            unit: "{registrations}");

        LoginAttempts = _meter.CreateCounter<long>(
            "resumeapp.auth.login_attempts",
            description: "Number of login attempts",
            unit: "{attempts}");

        OtpVerifications = _meter.CreateCounter<long>(
            "resumeapp.auth.otp_verifications",
            description: "Number of OTP verification attempts",
            unit: "{verifications}");

        PasswordResets = _meter.CreateCounter<long>(
            "resumeapp.auth.password_resets",
            description: "Number of password reset requests",
            unit: "{resets}");

        SocialLogins = _meter.CreateCounter<long>(
            "resumeapp.auth.social_logins",
            description: "Number of social login completions",
            unit: "{logins}");

        EducationsCreated = _meter.CreateCounter<long>(
            "resumeapp.data.educations_created",
            description: "Number of education entries created",
            unit: "{entries}");

        ExperiencesCreated = _meter.CreateCounter<long>(
            "resumeapp.data.experiences_created",
            description: "Number of experience entries created",
            unit: "{entries}");

        SkillsCreated = _meter.CreateCounter<long>(
            "resumeapp.data.skills_created",
            description: "Number of skills created",
            unit: "{entries}");

        ProjectsCreated = _meter.CreateCounter<long>(
            "resumeapp.data.projects_created",
            description: "Number of projects created",
            unit: "{entries}");
    }

    public void Dispose() => _meter.Dispose();
}
