namespace ResumeApp.Services;

public class AuthService
{
    public async Task<bool> LoginAsync(string email, string password, CancellationToken cancellationToken = default)
    {
        await Task.Delay(800, cancellationToken);

        return !string.IsNullOrWhiteSpace(email)
            && !string.IsNullOrWhiteSpace(password)
            && email.Contains('@');
    }
}
