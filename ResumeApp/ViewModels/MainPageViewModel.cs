using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ResumeApp.Services;
using ResumeApp.Views;

namespace ResumeApp.ViewModels;

public partial class MainPageViewModel : ObservableObject
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

    [ObservableProperty]
    private string profileImageUrl = "";

    [ObservableProperty]
    private bool isAiCoachEnabled;

    public string AppHeading { get; } = "AI Resume Builder";

    public string Greeting => $"Hi, {UserName} 👋";

    public ImageSource? ProfileImageSource =>
        !string.IsNullOrWhiteSpace(ProfileImagePath)
            ? ImageSource.FromFile(ProfileImagePath)
            : !string.IsNullOrWhiteSpace(ProfileImageUrl)
                ? ImageSource.FromUri(new Uri(ProfileImageUrl))
                : null;

    public MainPageViewModel(
        AuthService authService,
        IApiService apiService,
        ILocalStorageService localStorageService)
    {
        _authService = authService;
        _apiService = apiService;
        _localStorageService = localStorageService;
        _ = LoadUserInfoAsync();
    }

    private async Task LoadUserInfoAsync()
    {
        IsAiCoachEnabled = await _apiService.IsAiCoachEnabledAsync();

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
    private async Task Logout()
    {
        await _authService.LogoutAsync();
        await Shell.Current.GoToAsync("//login");
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

            if (result?.FullPath is null)
            {
                return;
            }

            ProfileImagePath = result.FullPath;
            await _localStorageService.SaveProfileImagePathAsync(ProfileImagePath);
            OnPropertyChanged(nameof(ProfileImageSource));

            var uploadedUrl = await _apiService.UploadProfileImageAsync(ProfileImagePath);
            if (!string.IsNullOrWhiteSpace(uploadedUrl))
            {
                ProfileImageUrl = uploadedUrl;
                OnPropertyChanged(nameof(ProfileImageSource));
            }
        }
        catch (Exception ex)
        {
            if (Shell.Current is not null)
            {
                await Shell.Current.DisplayAlert("Profile image", $"Profile image could not be updated.\n{ex.Message}", "OK");
            }
        }
    }

    [RelayCommand]
    private async Task GoToEducation() =>
        await Shell.Current.GoToAsync(nameof(EducationPage));

    [RelayCommand]
    private async Task GoToSkills() =>
        await Shell.Current.GoToAsync(nameof(SkillsPage));

    [RelayCommand]
    private async Task GoToProjects() =>
        await Shell.Current.GoToAsync(nameof(ProjectsPage));

    [RelayCommand]
    private async Task GoToGenerateResume() =>
        await Shell.Current.GoToAsync(nameof(GenerateResumePage));

    partial void OnUserNameChanged(string value)
    {
        OnPropertyChanged(nameof(Greeting));
    }
}
