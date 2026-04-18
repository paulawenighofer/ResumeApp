using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ResumeApp.Services;
using System.Collections.ObjectModel;
using System.Net.Mail;

namespace ResumeApp.ViewModels;

// Email is injected by Shell navigation from ForgotPasswordPage:
// reset-password?email=...
[QueryProperty(nameof(Email), "email")]
public partial class ResetPasswordViewModel : ObservableObject
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
    private string _code = "";
    private string _newPassword = "";
    private string _confirmPassword = "";
    private string _errorMessage = "";
    private bool _hasError;
    private bool _isBusy;
    private bool _resetSucceeded;
    private bool _isNewPasswordHidden = true;
    private bool _isConfirmPasswordHidden = true;
    private bool _showEmailSuggestions;
    private string _emailValidationMessage = "";
    private string _codeValidationMessage = "";
    private string _newPasswordValidationMessage = "";
    private string _confirmPasswordValidationMessage = "";
    private bool _hasEmailValidation;
    private bool _hasCodeValidation;
    private bool _hasNewPasswordValidation;
    private bool _hasConfirmPasswordValidation;

    public ResetPasswordViewModel(AuthService authService)
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

    public string Code
    {
        get => _code;
        set
        {
            if (SetProperty(ref _code, value))
            {
                ValidateCode();
            }
        }
    }

    public string NewPassword
    {
        get => _newPassword;
        set
        {
            if (SetProperty(ref _newPassword, value))
            {
                ValidateNewPassword();
                ValidateConfirmPassword();
            }
        }
    }

    public string ConfirmPassword
    {
        get => _confirmPassword;
        set
        {
            if (SetProperty(ref _confirmPassword, value))
            {
                ValidateConfirmPassword();
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

    public bool ResetSucceeded
    {
        get => _resetSucceeded;
        set => SetProperty(ref _resetSucceeded, value);
    }

    public bool IsNewPasswordHidden
    {
        get => _isNewPasswordHidden;
        set
        {
            if (SetProperty(ref _isNewPasswordHidden, value))
            {
                OnPropertyChanged(nameof(NewPasswordToggleIcon));
            }
        }
    }

    public bool IsConfirmPasswordHidden
    {
        get => _isConfirmPasswordHidden;
        set
        {
            if (SetProperty(ref _isConfirmPasswordHidden, value))
            {
                OnPropertyChanged(nameof(ConfirmPasswordToggleIcon));
            }
        }
    }

    public bool ShowEmailSuggestions
    {
        get => _showEmailSuggestions;
        set => SetProperty(ref _showEmailSuggestions, value);
    }

    public string EmailValidationMessage
    {
        get => _emailValidationMessage;
        set => SetProperty(ref _emailValidationMessage, value);
    }

    public string CodeValidationMessage
    {
        get => _codeValidationMessage;
        set => SetProperty(ref _codeValidationMessage, value);
    }

    public string NewPasswordValidationMessage
    {
        get => _newPasswordValidationMessage;
        set => SetProperty(ref _newPasswordValidationMessage, value);
    }

    public string ConfirmPasswordValidationMessage
    {
        get => _confirmPasswordValidationMessage;
        set => SetProperty(ref _confirmPasswordValidationMessage, value);
    }

    public bool HasEmailValidation
    {
        get => _hasEmailValidation;
        set => SetProperty(ref _hasEmailValidation, value);
    }

    public bool HasCodeValidation
    {
        get => _hasCodeValidation;
        set => SetProperty(ref _hasCodeValidation, value);
    }

    public bool HasNewPasswordValidation
    {
        get => _hasNewPasswordValidation;
        set => SetProperty(ref _hasNewPasswordValidation, value);
    }

    public bool HasConfirmPasswordValidation
    {
        get => _hasConfirmPasswordValidation;
        set => SetProperty(ref _hasConfirmPasswordValidation, value);
    }

    public string NewPasswordToggleIcon => IsNewPasswordHidden ? "\uf06e" : "\uf070";

    public string ConfirmPasswordToggleIcon => IsConfirmPasswordHidden ? "\uf06e" : "\uf070";

    public ObservableCollection<string> EmailSuggestions { get; } = new();

    [RelayCommand]
    private void ToggleNewPasswordVisibility() => IsNewPasswordHidden = !IsNewPasswordHidden;

    [RelayCommand]
    private void ToggleConfirmPasswordVisibility() => IsConfirmPasswordHidden = !IsConfirmPasswordHidden;

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
    private async Task ResetPassword()
    {
        if (!ValidateInputs())
        {
            ErrorMessage = "Please correct the highlighted fields.";
            HasError = true;
            return;
        }

        try
        {
            IsBusy = true;
            HasError = false;

            var (success, error) = await _authService.ResetPasswordAsync(Email, Code, NewPassword);

            if (success)
                ResetSucceeded = true;
            else
            {
                ErrorMessage = error ?? "Invalid or expired code.";
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

    [RelayCommand]
    private async Task GoToLogin()
    {
        await Shell.Current.GoToAsync("//login");
    }

    private bool ValidateInputs()
    {
        ValidateEmail();
        ValidateCode();
        ValidateNewPassword();
        ValidateConfirmPassword();

        return !HasEmailValidation &&
               !HasCodeValidation &&
               !HasNewPasswordValidation &&
               !HasConfirmPasswordValidation;
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

    private void ValidateCode()
    {
        if (string.IsNullOrWhiteSpace(Code))
        {
            CodeValidationMessage = "Reset code is required.";
            HasCodeValidation = true;
            return;
        }

        if (Code.Length != 6 || !Code.All(char.IsDigit))
        {
            CodeValidationMessage = "Reset code must be 6 digits.";
            HasCodeValidation = true;
            return;
        }

        CodeValidationMessage = string.Empty;
        HasCodeValidation = false;
    }

    private void ValidateNewPassword()
    {
        if (string.IsNullOrWhiteSpace(NewPassword))
        {
            NewPasswordValidationMessage = "New password is required.";
            HasNewPasswordValidation = true;
            return;
        }

        if (NewPassword.Length < 8)
        {
            NewPasswordValidationMessage = "Use at least 8 characters.";
            HasNewPasswordValidation = true;
            return;
        }

        NewPasswordValidationMessage = string.Empty;
        HasNewPasswordValidation = false;
    }

    private void ValidateConfirmPassword()
    {
        if (string.IsNullOrWhiteSpace(ConfirmPassword))
        {
            ConfirmPasswordValidationMessage = "Please confirm the new password.";
            HasConfirmPasswordValidation = true;
            return;
        }

        if (!string.Equals(NewPassword, ConfirmPassword, StringComparison.Ordinal))
        {
            ConfirmPasswordValidationMessage = "Passwords don't match.";
            HasConfirmPasswordValidation = true;
            return;
        }

        ConfirmPasswordValidationMessage = string.Empty;
        HasConfirmPasswordValidation = false;
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
}
