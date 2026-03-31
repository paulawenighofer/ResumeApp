using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ResumeApp.Services;

namespace ResumeApp.ViewModels;

public partial class RegisterViewModel : ObservableObject
{
    private readonly AuthService _authService;

    public RegisterViewModel(AuthService authService)
    {
        _authService = authService;
    }

    [ObservableProperty]
    private string firstName = "";

    [ObservableProperty]
    private string lastName = "";

    [ObservableProperty]
    private string email = "";

    [ObservableProperty]
    private string password = "";

    [ObservableProperty]
    private string confirmPassword = "";

    [ObservableProperty]
    private string errorMessage = "";

    [ObservableProperty]
    private bool hasError;

    [ObservableProperty]
    private bool isBusy;

    [RelayCommand]
    private async Task Register()
    {
        // Basic validation
        if (string.IsNullOrWhiteSpace(FirstName) ||
            string.IsNullOrWhiteSpace(LastName) ||
            string.IsNullOrWhiteSpace(Email) ||
            string.IsNullOrWhiteSpace(Password))
        {
            ErrorMessage = "Please fill in all fields.";
            HasError = true;
            return;
        }

        if (Password != ConfirmPassword)
        {
            ErrorMessage = "Passwords don't match.";
            HasError = true;
            return;
        }

        if (Password.Length < 8)
        {
            ErrorMessage = "Password must be at least 8 characters.";
            HasError = true;
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
}