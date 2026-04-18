using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ResumeApp.Services;
using ResumeApp.Views;

namespace ResumeApp.ViewModels;

public partial class ProfileViewModel : ObservableObject
{
    private readonly AuthService _authService;
    private readonly IApiService _apiService;
    private readonly ILocalStorageService _localStorageService;

    [ObservableProperty]
    private string userName = "User";

    [ObservableProperty]
    private string userEmail = "";

    [ObservableProperty]
    private string profileImagePath = "";

    private string profileImageUrl = "";
    public string ProfileImageUrl
    {
        get => profileImageUrl;
        set
        {
            if (SetProperty(ref profileImageUrl, value))
            {
                OnPropertyChanged(nameof(ProfileImageSource));
                OnPropertyChanged(nameof(HasProfileImage));
            }
        }
    }

    public bool HasProfileImage => !string.IsNullOrWhiteSpace(ProfileImagePath) || !string.IsNullOrWhiteSpace(ProfileImageUrl);

    public ImageSource? ProfileImageSource =>
        !string.IsNullOrWhiteSpace(ProfileImageUrl)
            ? ImageSource.FromUri(new Uri(ProfileImageUrl))
            : !string.IsNullOrWhiteSpace(ProfileImagePath)
                ? ImageSource.FromFile(ProfileImagePath)
                : null;

    public ProfileViewModel(AuthService authService, IApiService apiService, ILocalStorageService localStorageService)
    {
        _authService = authService;
        _apiService = apiService;
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

        var savedImageUrl = await _localStorageService.LoadProfileImageUrlAsync();
        if (!string.IsNullOrWhiteSpace(savedImageUrl))
        {
            ProfileImageUrl = savedImageUrl;
            ProfileImagePath = string.Empty;
            OnPropertyChanged(nameof(ProfileImageSource));
            OnPropertyChanged(nameof(HasProfileImage));
            return;
        }

        var savedImagePath = await _localStorageService.LoadProfileImagePathAsync();
        if (!string.IsNullOrWhiteSpace(savedImagePath))
        {
            ProfileImagePath = savedImagePath;
            OnPropertyChanged(nameof(ProfileImageSource));
            OnPropertyChanged(nameof(HasProfileImage));
        }
    }

    partial void OnProfileImagePathChanged(string value)
    {
        OnPropertyChanged(nameof(ProfileImageSource));
        OnPropertyChanged(nameof(HasProfileImage));
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

            ProfileImageUrl = string.Empty;
            ProfileImagePath = result.FullPath;
            await _localStorageService.SaveProfileImagePathAsync(ProfileImagePath);

            var uploadedUrl = await _apiService.UploadProfileImageAsync(ProfileImagePath);
            if (!string.IsNullOrWhiteSpace(uploadedUrl))
            {
                ProfileImageUrl = uploadedUrl;
                ProfileImagePath = string.Empty;

                await _localStorageService.SaveProfileImageUrlAsync(ProfileImageUrl);
                await _localStorageService.SaveProfileImagePathAsync(null);
            }
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
    private async Task GoToCertifications() =>
        await Shell.Current.GoToAsync(nameof(CertificationsPage));

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
