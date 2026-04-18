using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ResumeApp.Services;

namespace ResumeApp.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
    private readonly AuthService _authService;
    private readonly ILocalStorageService _localStorageService;

    [ObservableProperty]
    private bool isDarkMode = Application.Current?.RequestedTheme == AppTheme.Dark;

    public string AppVersion => AppInfo.VersionString;

    public SettingsViewModel(AuthService authService, ILocalStorageService localStorageService)
    {
        _authService = authService;
        _localStorageService = localStorageService;
    }

    partial void OnIsDarkModeChanged(bool value)
    {
        if (Application.Current is not null)
            Application.Current.UserAppTheme = value ? AppTheme.Dark : AppTheme.Light;
    }

    [RelayCommand]
    private async Task Logout()
    {
        bool confirmed = await Shell.Current.DisplayAlert(
            "Sign out",
            "Are you sure you want to sign out?",
            "Sign out", "Cancel");

        if (!confirmed) return;

        await _authService.LogoutAsync();
        await Shell.Current.GoToAsync("//login");
    }

    [RelayCommand]
    private async Task ClearCache()
    {
        bool confirmed = await Shell.Current.DisplayAlert(
            "Clear cache",
            "This will remove locally saved drafts. Are you sure?",
            "Clear", "Cancel");

        if (!confirmed) return;

        await _localStorageService.ClearAllLocalDataAsync();
        await Shell.Current.DisplayAlert("Done", "Cache cleared.", "OK");
    }
}
