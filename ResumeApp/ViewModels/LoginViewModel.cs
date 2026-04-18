using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ResumeApp.Services;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Net.Mail;

namespace ResumeApp.ViewModels;

public partial class LoginViewModel : ObservableObject
{
    private static readonly string[] PopularEmailDomains =
    {
        "gmail.com",
        "hotmail.com",
        "outlook.com",
        "yahoo.com"
    };

    private readonly AuthService _authService;

    private string _email = "";
    private string _password = "";
    private string _errorMessage = "";
    private bool _hasError;
    private bool _isBusy;
    private bool _isPasswordHidden = true;
    private string _emailValidationMessage = "";
    private string _passwordValidationMessage = "";
    private bool _hasEmailValidation;
    private bool _hasPasswordValidation;
    private bool _showEmailSuggestions;

    public LoginViewModel(AuthService authService)
    {
        _authService = authService;
    }

    public string Email
    {
        get => _email;
        set
        {
            if (SetProperty(ref _email, value))
            {
                ValidateEmail();
                UpdateEmailSuggestions(value);
            }
        }
    }

    public string Password
    {
        get => _password;
        set
        {
            if (SetProperty(ref _password, value))
            {
                ValidatePassword();
            }
        }
    }

    public string ErrorMessage
    {
        get => _errorMessage;
        set => SetProperty(ref _errorMessage, value);
    }

    public bool HasError
    {
        get => _hasError;
        set => SetProperty(ref _hasError, value);
    }

    public bool IsBusy
    {
        get => _isBusy;
        set => SetProperty(ref _isBusy, value);
    }

    public bool IsPasswordHidden
    {
        get => _isPasswordHidden;
        set
        {
            if (SetProperty(ref _isPasswordHidden, value))
            {
                OnPropertyChanged(nameof(PasswordToggleIcon));
            }
        }
    }

    public string EmailValidationMessage
    {
        get => _emailValidationMessage;
        set => SetProperty(ref _emailValidationMessage, value);
    }

    public string PasswordValidationMessage
    {
        get => _passwordValidationMessage;
        set => SetProperty(ref _passwordValidationMessage, value);
    }

    public bool HasEmailValidation
    {
        get => _hasEmailValidation;
        set => SetProperty(ref _hasEmailValidation, value);
    }

    public bool HasPasswordValidation
    {
        get => _hasPasswordValidation;
        set => SetProperty(ref _hasPasswordValidation, value);
    }

    public bool ShowEmailSuggestions
    {
        get => _showEmailSuggestions;
        set => SetProperty(ref _showEmailSuggestions, value);
    }

    public string PasswordToggleIcon => IsPasswordHidden ? "\uf06e" : "\uf070";

    public ObservableCollection<string> EmailSuggestions { get; } = new();

    [RelayCommand]
    private void TogglePasswordVisibility()
    {
        IsPasswordHidden = !IsPasswordHidden;
    }

    [RelayCommand]
    private void ApplyEmailSuggestion(string suggestion)
    {
        if (string.IsNullOrWhiteSpace(suggestion))
        {
            return;
        }

        Email = suggestion;
        ShowEmailSuggestions = false;
    }

    [RelayCommand]
    private async Task Login()
    {
        if (!ValidateInputs())
        {
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

    [RelayCommand]
    private async Task GoogleLogin() => await SocialLoginAsync("google");

    [RelayCommand]
    private async Task LinkedInLogin() => await SocialLoginAsync("linkedin");

    [RelayCommand]
    private async Task GitHubLogin() => await SocialLoginAsync("github");

    private async Task SocialLoginAsync(string provider)
    {
        try
        {
            IsBusy = true;
            HasError = false;

            string? token = null;

#if WINDOWS
            token = await DesktopAuthHelper.AuthenticateAsync(
                _authService.BaseUrl, provider);
#else
            var apiBaseUrl = _authService.BaseUrl;
            var challengeUrl = new Uri($"{apiBaseUrl}/api/auth/{provider}-challenge");
            var callbackUrl = new Uri("myresumebuilder://auth");

            var authResult = await WebAuthenticator.Default.AuthenticateAsync(
                challengeUrl, callbackUrl);

            if (authResult.Properties.TryGetValue("token", out var t))
                token = t;
            else if (TryGetPropertyIgnoreCase(authResult.Properties, "token", out var tokenValue))
                token = tokenValue;
            else if (authResult.Properties.TryGetValue("error", out var error))
            {
                await ShowErrorAsync($"Login failed: {error}");
                return;
            }
            else if (TryGetPropertyIgnoreCase(authResult.Properties, "error", out var errorValue))
            {
                await ShowErrorAsync($"Login failed: {errorValue}");
                return;
            }
#endif

            if (!string.IsNullOrEmpty(token))
            {
                await SecureStorage.SetAsync("auth_token", token);
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
            await ShowErrorAsync("Login was cancelled or the callback did not complete.");
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

    private bool ValidateInputs()
    {
        ValidateEmail();
        ValidatePassword();

        if (HasEmailValidation || HasPasswordValidation)
        {
            ErrorMessage = "Please correct the highlighted fields.";
            HasError = true;
            return false;
        }

        HasError = false;
        ErrorMessage = string.Empty;
        return true;
    }

    private void ValidateEmail()
    {
        if (string.IsNullOrWhiteSpace(Email))
        {
            EmailValidationMessage = "Email is required.";
            HasEmailValidation = true;
            return;
        }

        try
        {
            _ = new MailAddress(Email);
            EmailValidationMessage = string.Empty;
            HasEmailValidation = false;
        }
        catch
        {
            EmailValidationMessage = "Please enter a valid email.";
            HasEmailValidation = true;
        }
    }

    private void ValidatePassword()
    {
        if (string.IsNullOrWhiteSpace(Password))
        {
            PasswordValidationMessage = "Password is required.";
            HasPasswordValidation = true;
            return;
        }

        PasswordValidationMessage = string.Empty;
        HasPasswordValidation = false;
    }

    private void UpdateEmailSuggestions(string value)
    {
        EmailSuggestions.Clear();

        if (string.IsNullOrWhiteSpace(value))
        {
            ShowEmailSuggestions = false;
            return;
        }

        var trimmed = value.Trim();

        if (trimmed.Contains('@'))
        {
            var parts = trimmed.Split('@');
            if (parts.Length != 2 || string.IsNullOrWhiteSpace(parts[0]))
            {
                ShowEmailSuggestions = false;
                return;
            }

            var local = parts[0];
            var typedDomain = parts[1];

            foreach (var domain in PopularEmailDomains.Where(d => d.StartsWith(typedDomain, StringComparison.OrdinalIgnoreCase)))
            {
                EmailSuggestions.Add($"{local}@{domain}");
            }
        }
        else
        {
            foreach (var domain in PopularEmailDomains)
            {
                EmailSuggestions.Add($"{trimmed}@{domain}");
            }
        }

        ShowEmailSuggestions = EmailSuggestions.Count > 0;
    }

    private async Task ShowErrorAsync(string message)
    {
        ErrorMessage = message;
        HasError = true;
        await ToastService.ShowAsync(message, isError: true, durationMilliseconds: 4500);
    }

    private static bool TryGetPropertyIgnoreCase(
        IDictionary<string, string> properties,
        string key,
        out string? value)
    {
        foreach (var pair in properties)
        {
            if (string.Equals(pair.Key, key, StringComparison.OrdinalIgnoreCase))
            {
                value = pair.Value;
                return true;
            }
        }

        value = null;
        return false;
    }
}