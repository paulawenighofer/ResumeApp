using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ResumeApp.Services;

namespace ResumeApp.ViewModels;

public partial class ForgotPasswordViewModel : ObservableObject
{
    private readonly AuthService _authService;

    public ForgotPasswordViewModel(AuthService authService)
    {
        _authService = authService;
    }

    [ObservableProperty]
    private string email = "";

    [ObservableProperty]
    private string errorMessage = "";

    [ObservableProperty]
    private bool hasError;

    [ObservableProperty]
    private bool isBusy;

    // Set when the account exists but email isn't verified yet
    [ObservableProperty]
    private bool requiresVerification;

    [ObservableProperty]
    private string? unverifiedEmail;

    // True after a successful API call — shows the "check your email" confirmation state
    [ObservableProperty]
    private bool codeSent;

    [RelayCommand]
    private async Task SendResetLink()
    {
        if (string.IsNullOrWhiteSpace(Email))
        {
            await ShowErrorAsync("Please enter your email address.");
            return;
        }

        try
        {
            IsBusy = true;
            HasError = false;
            RequiresVerification = false;

            var result = await _authService.ForgotPasswordAsync(Email);

            if (result.Success)
            {
                // Stay on this page and show a confirmation — do NOT navigate yet.
                // The API returns 200 for both real and unknown emails (security),
                // so navigating immediately would drop unknown-email users into the
                // reset form waiting for a code that never arrives.
                CodeSent = true;
            }
            else if (result.RequiresVerification)
            {
                UnverifiedEmail = result.Email ?? Email;
                RequiresVerification = true;
            }
            else
            {
                await ShowErrorAsync(result.Message ?? "Something went wrong. Please try again.");
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
    private async Task EnterCode()
    {
        await Shell.Current.GoToAsync($"//reset-password?email={Uri.EscapeDataString(Email)}");
    }

    [RelayCommand]
    private void TryDifferentEmail()
    {
        Email = "";
        CodeSent = false;
        HasError = false;
    }

    [RelayCommand]
    private async Task GoToVerify()
    {
        var emailToVerify = UnverifiedEmail ?? Email;
        await Shell.Current.GoToAsync($"///otp?email={Uri.EscapeDataString(emailToVerify)}");
    }

    [RelayCommand]
    private async Task GoToLogin()
    {
        await Shell.Current.GoToAsync("//login");
    }

    private async Task ShowErrorAsync(string message)
    {
        ErrorMessage = message;
        HasError = true;
        await ToastService.ShowAsync(message, isError: true, durationMilliseconds: 4500);
    }
}
