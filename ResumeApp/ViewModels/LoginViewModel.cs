using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ResumeApp.Services;

namespace ResumeApp.ViewModels;

public partial class LoginViewModel : ObservableObject
{
    private readonly AuthService _authService;

    public LoginViewModel(AuthService authService)
    {
        _authService = authService;
    }

    [ObservableProperty]
    private string email = "";

    [ObservableProperty]
    private string password = "";

    [ObservableProperty]
    private string errorMessage = "";

    [ObservableProperty]
    private bool hasError;

    [ObservableProperty]
    private bool isBusy;

    [RelayCommand]
    private async Task Login()
    {
        if (string.IsNullOrWhiteSpace(Email) || string.IsNullOrWhiteSpace(Password))
        {
            await ShowErrorAsync("Please enter your email and password.");
            return;
        }

        try
        {
            IsBusy = true;
            HasError = false;

            var result = await _authService.LoginAsync(Email, Password);

            if (result.Success)
                await Shell.Current.GoToAsync("//main/home");
            else if (result.RequiresVerification)
                await Shell.Current.GoToAsync($"///otp?email={Uri.EscapeDataString(result.Email ?? Email)}");
            else
            {
                await ShowErrorAsync("Invalid email or password.");
            }
        }
        catch (Exception)
        {
            await ShowErrorAsync("Something went wrong. Please try again.");
        }
        finally
        {
            IsBusy = false;
        }
    }

    // All three social logins use the exact same method — only the
    // provider name changes, which determines which backend endpoint is hit
    [RelayCommand]
    private async Task GoogleLogin() => await SocialLoginAsync("google");

    [RelayCommand]
    private async Task LinkedInLogin() => await SocialLoginAsync("linkedin");

    [RelayCommand]
    private async Task GitHubLogin() => await SocialLoginAsync("github");

    /// <summary>
    /// Handles social login for all providers on all platforms.
    ///
    /// Uses compile-time #if WINDOWS to pick the right approach:
    /// - Windows: DesktopAuthHelper (local HTTP listener)
    /// - Mobile: WebAuthenticator (OS-level URL scheme handling)
    ///
    /// WebAuthenticator.AuthenticateAsync (mobile):
    /// 1. Opens a browser to the challengeUrl (your backend)
    /// 2. Your backend redirects to the provider's login page
    /// 3. User signs in → provider redirects back to your backend
    /// 4. Your backend generates JWT → redirects to myresumebuilder://auth?token=xxx
    /// 5. WebAuthenticator catches that redirect and returns the parameters
    /// </summary>
    private async Task SocialLoginAsync(string provider)
    {
        try
        {
            IsBusy = true;
            HasError = false;

            string? token = null;

#if WINDOWS
            // Windows: spin up a local HTTP listener as the callback
            token = await DesktopAuthHelper.AuthenticateAsync(
                _authService.BaseUrl, provider);
#else
            // Android/iOS: use WebAuthenticator
            var apiBaseUrl = _authService.BaseUrl;
            var challengeUrl = new Uri($"{apiBaseUrl}/api/auth/{provider}-challenge");
            var callbackUrl = new Uri("myresumebuilder://auth");

            var authResult = await WebAuthenticator.Default.AuthenticateAsync(
                challengeUrl, callbackUrl);

            if (authResult.Properties.TryGetValue("token", out var t))
                token = t;
            else if (authResult.Properties.TryGetValue("error", out var error))
            {
                await ShowErrorAsync($"Login failed: {error}");
                return;
            }
#endif

            if (!string.IsNullOrEmpty(token))
            {
                await SecureStorage.SetAsync("auth_token", token);
                // Social login only returns a token — fetch profile separately
                // so name and email are available on the main page
                await _authService.FetchAndSaveUserInfoAsync();
                await Shell.Current.GoToAsync("//main/home");
            }
            else
            {
                await ShowErrorAsync("Login was cancelled or failed.");
            }
        }
        catch (TaskCanceledException)
        {
            // User cancelled — do nothing
        }
        catch (Exception)
        {
            await ShowErrorAsync("Social login failed. Please try again.");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task GoToRegister()
    {
        await Shell.Current.GoToAsync("//register");
    }

    [RelayCommand]
    private async Task GoToForgotPassword()
    {
        await Shell.Current.GoToAsync("//forgot-password");
    }

    private async Task ShowErrorAsync(string message)
    {
        ErrorMessage = message;
        HasError = true;
        await ToastService.ShowAsync(message, isError: true, durationMilliseconds: 4500);
    }
}