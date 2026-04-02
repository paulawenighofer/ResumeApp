using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ResumeApp.Services;
using ResumeApp.Views;

namespace ResumeApp.ViewModels;

public partial class ProfileViewModel : ObservableObject
{
    private readonly AuthService _authService;
    private readonly ILocalStorageService _localStorageService;

    [ObservableProperty]
    private string userName = "User";

    [ObservableProperty]
    private string userEmail = "";

    [ObservableProperty]
    private string profileImagePath = "";

    public ImageSource? ProfileImageSource =>
        !string.IsNullOrWhiteSpace(ProfileImagePath)
            ? ImageSource.FromFile(ProfileImagePath)
            : null;

    public ProfileViewModel(AuthService authService, ILocalStorageService localStorageService)
    {
        _authService = authService;
        _localStorageService = localStorageService;
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

        var savedImagePath = await _localStorageService.LoadProfileImagePathAsync();
        if (!string.IsNullOrWhiteSpace(savedImagePath))
        {
            ProfileImagePath = savedImagePath;
            OnPropertyChanged(nameof(ProfileImageSource));
        }
    }

    [RelayCommand]
    private async Task PickProfileImage()
    {
        try
        {
            var result = await FilePicker.Default.PickAsync(new PickOptions
            {
                FileTypes = FilePickerFileType.Images,
                PickerTitle = "Select profile image"
            });

            if (result?.FullPath is null) return;

            ProfileImagePath = result.FullPath;
            await _localStorageService.SaveProfileImagePathAsync(ProfileImagePath);
            OnPropertyChanged(nameof(ProfileImageSource));
        }
        catch (Exception ex)
        {
            if (Shell.Current is not null)
                await Shell.Current.DisplayAlert("Profile image", ex.Message, "OK");
        }
    }

    [RelayCommand]
    private async Task GoToEducation() =>
        await Shell.Current.GoToAsync(nameof(EducationPage));

    [RelayCommand]
    private async Task GoToExperience() =>
        await Shell.Current.GoToAsync(nameof(ExperiencePage));

    [RelayCommand]
    private async Task GoToSkills() =>
        await Shell.Current.GoToAsync(nameof(SkillsPage));

    [RelayCommand]
    private async Task GoToProjects() =>
        await Shell.Current.GoToAsync(nameof(ProjectsPage));

    [RelayCommand]
    private async Task Logout()
    {
        await _authService.LogoutAsync();
        await Shell.Current.GoToAsync("//login");
    }
}
