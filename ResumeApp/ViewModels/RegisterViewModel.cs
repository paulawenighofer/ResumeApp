using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ResumeApp.Services;
using System.Collections.ObjectModel;
using System.Net.Mail;

namespace ResumeApp.ViewModels;

public partial class RegisterViewModel : ObservableObject
{
    private static readonly string[] PopularEmailDomains =
    {
        "gmail.com",
        "hotmail.com",
        "outlook.com",
        "yahoo.com"
    };

    private readonly AuthService _authService;

    private string _firstName = "";
    private string _lastName = "";
    private string _email = "";
    private string _password = "";
    private string _confirmPassword = "";
    private string _errorMessage = "";
    private bool _hasError;
    private bool _isBusy;
    private bool _isPasswordHidden = true;
    private bool _isConfirmPasswordHidden = true;
    private bool _showEmailSuggestions;
    private string _firstNameValidationMessage = "";
    private string _lastNameValidationMessage = "";
    private string _emailValidationMessage = "";
    private string _passwordValidationMessage = "";
    private string _confirmPasswordValidationMessage = "";
    private bool _hasFirstNameValidation;
    private bool _hasLastNameValidation;
    private bool _hasEmailValidation;
    private bool _hasPasswordValidation;
    private bool _hasConfirmPasswordValidation;

    public RegisterViewModel(AuthService authService)
    {
        _authService = authService;
    }

    public string FirstName
    {
        get => _firstName;
        set
        {
            if (SetProperty(ref _firstName, value))
            {
                ValidateFirstName();
            }
        }
    }

    public string LastName
    {
        get => _lastName;
        set
        {
            if (SetProperty(ref _lastName, value))
            {
                ValidateLastName();
            }
        }
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

    public string FirstNameValidationMessage
    {
        get => _firstNameValidationMessage;
        set => SetProperty(ref _firstNameValidationMessage, value);
    }

    public string LastNameValidationMessage
    {
        get => _lastNameValidationMessage;
        set => SetProperty(ref _lastNameValidationMessage, value);
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

    public string ConfirmPasswordValidationMessage
    {
        get => _confirmPasswordValidationMessage;
        set => SetProperty(ref _confirmPasswordValidationMessage, value);
    }

    public bool HasFirstNameValidation
    {
        get => _hasFirstNameValidation;
        set => SetProperty(ref _hasFirstNameValidation, value);
    }

    public bool HasLastNameValidation
    {
        get => _hasLastNameValidation;
        set => SetProperty(ref _hasLastNameValidation, value);
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

    public bool HasConfirmPasswordValidation
    {
        get => _hasConfirmPasswordValidation;
        set => SetProperty(ref _hasConfirmPasswordValidation, value);
    }

    public string PasswordToggleIcon => IsPasswordHidden ? "\uf06e" : "\uf070";

    public string ConfirmPasswordToggleIcon => IsConfirmPasswordHidden ? "\uf06e" : "\uf070";

    public ObservableCollection<string> EmailSuggestions { get; } = new();

    [RelayCommand]
    private void TogglePasswordVisibility() => IsPasswordHidden = !IsPasswordHidden;

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
    private async Task Register()
    {
        if (!ValidateInputs())
        {
            return;
        }

        try
        {
            IsBusy = true;
            HasError = false;
            ErrorMessage = string.Empty;

            var result = await _authService.RegisterAsync(
                FirstName, LastName, Email, Password);

            if (result.Success && !string.IsNullOrWhiteSpace(result.Email))
            {
                try
                {
                    await Shell.Current.GoToAsync($"///otp?email={Uri.EscapeDataString(result.Email)}");
                }
                catch
                {
                    ErrorMessage = "Couldn't open verification screen. Please try again.";
                    HasError = true;
                }
            }
            else
            {
                ErrorMessage = string.IsNullOrWhiteSpace(result.ErrorMessage)
                    ? "Registration failed. Please try again."
                    : result.ErrorMessage;
                HasError = true;
            }
        }
        catch
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
        ValidateFirstName();
        ValidateLastName();
        ValidateEmail();
        ValidatePassword();
        ValidateConfirmPassword();

        if (HasFirstNameValidation || HasLastNameValidation || HasEmailValidation || HasPasswordValidation || HasConfirmPasswordValidation)
        {
            ErrorMessage = "Please correct the highlighted fields.";
            HasError = true;
            return false;
        }

        HasError = false;
        ErrorMessage = string.Empty;
        return true;
    }

    private void ValidateFirstName()
    {
        if (string.IsNullOrWhiteSpace(FirstName))
        {
            FirstNameValidationMessage = "First name is required.";
            HasFirstNameValidation = true;
            return;
        }

        FirstNameValidationMessage = string.Empty;
        HasFirstNameValidation = false;
    }

    private void ValidateLastName()
    {
        if (string.IsNullOrWhiteSpace(LastName))
        {
            LastNameValidationMessage = "Last name is required.";
            HasLastNameValidation = true;
            return;
        }

        LastNameValidationMessage = string.Empty;
        HasLastNameValidation = false;
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

        if (Password.Length < 8)
        {
            PasswordValidationMessage = "Use at least 8 characters.";
            HasPasswordValidation = true;
            return;
        }

        PasswordValidationMessage = string.Empty;
        HasPasswordValidation = false;
    }

    private void ValidateConfirmPassword()
    {
        if (string.IsNullOrWhiteSpace(ConfirmPassword))
        {
            ConfirmPasswordValidationMessage = "Please confirm your password.";
            HasConfirmPasswordValidation = true;
            return;
        }

        if (!string.Equals(Password, ConfirmPassword, StringComparison.Ordinal))
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