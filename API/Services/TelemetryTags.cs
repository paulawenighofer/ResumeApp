namespace API.Services;

public static class TelemetryTags
{
    public static class Outcomes
    {
        public const string Success = "success";
        public const string Failure = "failure";
        public const string LockedOut = "locked_out";
        public const string Unverified = "unverified";
        public const string Requested = "requested";
    }

    public static class EmailTemplates
    {
        public const string Verification = "verification";
        public const string PasswordReset = "password_reset";
    }

    public static class Providers
    {
        public const string Google = "google";
        public const string LinkedIn = "linkedin";
        public const string GitHub = "github";
    }

    public static class Sections
    {
        public const string Education = "education";
        public const string Experience = "experience";
        public const string Skill = "skill";
        public const string Project = "project";
        public const string ProfileImage = "profile_image";
        public const string ProjectImage = "project_image";
    }

    public static class Actions
    {
        public const string Create = "create";
        public const string Update = "update";
        public const string Delete = "delete";
        public const string Upload = "upload";
    }
}
