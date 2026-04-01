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

    public ResetPasswordViewModel(AuthService authService)
    {
        _authService = authService;
    }

    [ObservableProperty]
    private string email = "";

    [ObservableProperty]
    private string code = "";

    [ObservableProperty]
    private string newPassword = "";

    [ObservableProperty]
    private string confirmPassword = "";

    [ObservableProperty]
    private string errorMessage = "";

    [ObservableProperty]
    private bool hasError;

    [ObservableProperty]
    private bool isBusy;

    [ObservableProperty]
    private bool resetSucceeded;

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
