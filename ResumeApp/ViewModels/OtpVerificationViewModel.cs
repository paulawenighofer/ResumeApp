using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ResumeApp.Services;

namespace ResumeApp.ViewModels;

[QueryProperty(nameof(Email), "email")]
public partial class OtpVerificationViewModel : ObservableObject
{
    private readonly AuthService _authService;

    public OtpVerificationViewModel(AuthService authService)
    {
        _authService = authService;
    }

    [ObservableProperty]
    private string email = "";

    [ObservableProperty]
    private string code = "";

    [ObservableProperty]
    private string errorMessage = "";

    [ObservableProperty]
    private bool hasError;

    [ObservableProperty]
    private bool isBusy;

    [ObservableProperty]
    private string resendMessage = "";

    [ObservableProperty]
    private bool hasResendMessage;

    [RelayCommand]
    private async Task Verify()
    {
        if (string.IsNullOrWhiteSpace(Code) || Code.Length != 6)
        {
            ErrorMessage = "Please enter the 6-digit code from your email.";
            HasError = true;
            return;
        }

        try
        {
            IsBusy = true;
            HasError = false;
            HasResendMessage = false;

            var success = await _authService.VerifyOtpAsync(Email, Code);

            if (success)
                await Shell.Current.GoToAsync("//main/home");
            else
            {
                ErrorMessage = "Invalid or expired code. Please try again.";
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
    private async Task Resend()
    {
        try
        {
            IsBusy = true;
            HasError = false;
            HasResendMessage = false;

            await _authService.ResendOtpAsync(Email);

            ResendMessage = "A new code has been sent to your email.";
            HasResendMessage = true;
        }
        catch (Exception)
        {
            ErrorMessage = "Could not resend the code. Please try again.";
            HasError = true;
        }
        finally
        {
            IsBusy = false;
        }
    }
}
