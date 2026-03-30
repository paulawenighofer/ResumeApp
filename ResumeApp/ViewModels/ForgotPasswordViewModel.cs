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

    [RelayCommand]
    private async Task SendResetLink()
    {
        if (string.IsNullOrWhiteSpace(Email))
        {
            ErrorMessage = "Please enter your email address.";
            HasError = true;
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
                await Shell.Current.GoToAsync($"//reset-password?email={Uri.EscapeDataString(Email)}");
            }
            else if (result.RequiresVerification)
            {
                UnverifiedEmail = result.Email ?? Email;
                RequiresVerification = true;
            }
            else
            {
                ErrorMessage = result.Message ?? "Something went wrong. Please try again.";
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
}
