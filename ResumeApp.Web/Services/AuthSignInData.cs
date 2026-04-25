namespace ResumeApp.Web.Services;

public record AuthSignInData(
    string Token,
    string Email,
    string FirstName,
    string LastName,
    string? ProfileImageUrl);
