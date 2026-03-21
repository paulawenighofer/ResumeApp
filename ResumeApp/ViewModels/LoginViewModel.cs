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
            ErrorMessage = "Please enter your email and password.";
            HasError = true;
            return;
        }

        try
        {
            IsBusy = true;
            HasError = false;

            var success = await _authService.LoginAsync(Email, Password);

            if (success)
                await Shell.Current.GoToAsync("//main");
            else
            {
                ErrorMessage = "Invalid email or password.";
                HasError = true;
            }
        }
        catch (Exception)
        {
            ErrorMessage = "Something went wrong. Please try again.";
            HasError = true;
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
    /// One method handles ALL social logins. The provider string maps to
    /// the backend endpoint: /api/auth/{provider}-challenge
    /// 
    /// WebAuthenticator.AuthenticateAsync:
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

            var apiBaseUrl = _authService.BaseUrl;
            var challengeUrl = new Uri($"{apiBaseUrl}/api/auth/{provider}-challenge");
            var callbackUrl = new Uri("myresumebuilder://auth");

            var authResult = await WebAuthenticator.Default.AuthenticateAsync(
                challengeUrl, callbackUrl);

            if (authResult.Properties.TryGetValue("token", out var token)
                && !string.IsNullOrEmpty(token))
            {
                await SecureStorage.SetAsync("auth_token", token);
                await Shell.Current.GoToAsync("//main");
            }
            else if (authResult.Properties.TryGetValue("error", out var error))
            {
                ErrorMessage = $"Login failed: {error}";
                HasError = true;
            }
        }
        catch (TaskCanceledException)
        {
            // User closed the browser / cancelled — do nothing
        }
        catch (Exception)
        {
            ErrorMessage = "Social login failed. Please try again.";
            HasError = true;
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
