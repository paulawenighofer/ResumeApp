using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ResumeApp.Services;

namespace ResumeApp.ViewModels;

public partial class MainPageViewModel : ObservableObject
{
    private readonly AuthService _authService;

    [ObservableProperty]
    private string userName = "User";

    [ObservableProperty]
    private string userEmail = "";

    public string AppHeading { get; } = "AI Resume Builder";

    public MainPageViewModel(AuthService authService)
    {
        _authService = authService;
        _ = LoadUserInfoAsync();
    }

    private async Task LoadUserInfoAsync()
    {
        var savedName = await SecureStorage.GetAsync("user_name");
        if (!string.IsNullOrWhiteSpace(savedName))
            UserName = savedName;

        var savedEmail = await SecureStorage.GetAsync("user_email");
        if (!string.IsNullOrWhiteSpace(savedEmail))
            UserEmail = savedEmail;
    }

    [RelayCommand]
    private async Task Logout()
    {
        await _authService.LogoutAsync();
        await Shell.Current.GoToAsync("//login");
    }
}
