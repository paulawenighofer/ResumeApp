#if !WINDOWS && !ANDROID && !IOS && !MACCATALYST && !TIZEN
namespace ResumeApp.Views;

public sealed class CertificationsPage;
public sealed class EducationPage;
public sealed class ExperiencePage;
public sealed class ForgotPasswordPage;
public sealed class GenerateResumePage;
public sealed class LoginPage;
public sealed class MainPage;
public sealed class OtpVerificationPage;
public sealed class ProfilePage;
public sealed class ProjectsPage;
public sealed class RegisterPage;
public sealed class ResetPasswordPage;
public sealed class ResumeDraftDetailPage;
public sealed class ResumeListPage;
public sealed class SettingsPage;
public sealed class SkillsPage;

namespace ResumeApp.Views.Controls;

public enum NavTab
{
    Home,
    Resume,
    Profile,
    Settings
}
#endif
