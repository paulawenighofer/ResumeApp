using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using ResumeApp.Services;

namespace ResumeApp.ViewModels;

public class LoginViewModel : INotifyPropertyChanged
{
    private readonly AuthService _authService;
    private string _email = string.Empty;
    private string _password = string.Empty;
    private string _errorMessage = string.Empty;
    private bool _isBusy;

    public LoginViewModel(AuthService authService)
    {
        _authService = authService;
        LoginCommand = new Command(async () => await LoginAsync(), () => !IsBusy);
        LinkedInLoginCommand = new Command(() => ShowNotImplemented("LinkedIn login"));
        GitHubLoginCommand = new Command(() => ShowNotImplemented("GitHub login"));
        GoToRegisterCommand = new Command(() => ShowNotImplemented("Sign up"));
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public string Email
    {
        get => _email;
        set => SetProperty(ref _email, value);
    }

    public string Password
    {
        get => _password;
        set => SetProperty(ref _password, value);
    }

    public string ErrorMessage
    {
        get => _errorMessage;
        private set
        {
            if (SetProperty(ref _errorMessage, value))
            {
                OnPropertyChanged(nameof(HasError));
            }
        }
    }

    public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);

    public bool IsBusy
    {
        get => _isBusy;
        private set
        {
            if (SetProperty(ref _isBusy, value))
            {
                ((Command)LoginCommand).ChangeCanExecute();
            }
        }
    }

    public ICommand LoginCommand { get; }
    public ICommand LinkedInLoginCommand { get; }
    public ICommand GitHubLoginCommand { get; }
    public ICommand GoToRegisterCommand { get; }

    private async Task LoginAsync()
    {
        if (IsBusy)
        {
            return;
        }

        ErrorMessage = string.Empty;

        if (string.IsNullOrWhiteSpace(Email) || string.IsNullOrWhiteSpace(Password))
        {
            ErrorMessage = "Enter both email and password.";
            return;
        }

        try
        {
            IsBusy = true;

            var isAuthenticated = await _authService.LoginAsync(Email, Password);
            if (!isAuthenticated)
            {
                ErrorMessage = "Login failed. Check your credentials.";
                return;
            }

            await Shell.Current.GoToAsync($"//{nameof(MainPage)}");
        }
        catch (Exception)
        {
            ErrorMessage = "Something went wrong while signing in.";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void ShowNotImplemented(string provider)
    {
        ErrorMessage = $"{provider} is not connected yet in the MAUI app.";
    }

    private bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return false;
        }

        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
