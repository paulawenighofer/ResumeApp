using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ResumeApp.Services;

namespace ResumeApp.ViewModels;

// Email is injected by Shell navigation from ForgotPasswordPage:
// reset-password?email=...
[QueryProperty(nameof(Email), "email")]
public partial class ResetPasswordViewModel : ObservableObject
{
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

    public ResetPasswordViewModel(AuthService authService)
    {
        _authService = authService;
    }

    public string Email
    {
        get => _email;
        set => SetProperty(ref _email, value);
    }

    public string Code
    {
        get => _code;
        set => SetProperty(ref _code, value);
    }

    public string NewPassword
    {
        get => _newPassword;
        set => SetProperty(ref _newPassword, value);
    }

    public string ConfirmPassword
    {
        get => _confirmPassword;
        set => SetProperty(ref _confirmPassword, value);
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

    public string NewPasswordToggleIcon => IsNewPasswordHidden ? "\uf06e" : "\uf070";

    public string ConfirmPasswordToggleIcon => IsConfirmPasswordHidden ? "\uf06e" : "\uf070";

    [RelayCommand]
    private void ToggleNewPasswordVisibility() => IsNewPasswordHidden = !IsNewPasswordHidden;

    [RelayCommand]
    private void ToggleConfirmPasswordVisibility() => IsConfirmPasswordHidden = !IsConfirmPasswordHidden;

    [RelayCommand]
    private async Task ResetPassword()
    {
        if (string.IsNullOrWhiteSpace(Code) || Code.Length != 6)
        {
            ErrorMessage = "Please enter the 6-digit reset code.";
            HasError = true;
            return;
        }

        if (string.IsNullOrWhiteSpace(NewPassword) || string.IsNullOrWhiteSpace(ConfirmPassword))
        {
            ErrorMessage = "Please fill in both password fields.";
            HasError = true;
            return;
        }

        if (NewPassword != ConfirmPassword)
        {
            ErrorMessage = "Passwords don't match.";
            HasError = true;
            return;
        }

        if (NewPassword.Length < 8)
        {
            ErrorMessage = "Password must be at least 8 characters.";
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
}
