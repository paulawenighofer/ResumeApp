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
    private double educationCompletionProgress;

    [ObservableProperty]
    private string educationCompletionText = "0%";

    [ObservableProperty]
    private int educationEntryCount;

    [ObservableProperty]
    private double experienceCompletionProgress;

    [ObservableProperty]
    private string experienceCompletionText = "0%";

    [ObservableProperty]
    private int experienceEntryCount;

    public bool HasProfileImage => !string.IsNullOrWhiteSpace(ProfileImagePath) || !string.IsNullOrWhiteSpace(ProfileImageUrl);

    public string AppHeading { get; } = "AI Resume Builder";

    public string Greeting => $"Hi, {UserName} 👋";

    public ImageSource? ProfileImageSource =>
        !string.IsNullOrWhiteSpace(ProfileImageUrl)
            ? ImageSource.FromUri(new Uri(ProfileImageUrl))
            : !string.IsNullOrWhiteSpace(ProfileImagePath)
                ? ImageSource.FromFile(ProfileImagePath)
                : null;

    public MainPageViewModel(
        AuthService authService,
        IApiService apiService,
        ILocalStorageService localStorageService)
    {
        _authService = authService;
        _apiService = apiService;
        _localStorageService = localStorageService;
        _ = RefreshAsync();
    }

    public async Task RefreshAsync()
    {
        await LoadUserInfoAsync();
        await LoadProfileCompletionAsync();
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
            ProfileImageUrl = string.Empty;
            OnPropertyChanged(nameof(ProfileImageSource));
            OnPropertyChanged(nameof(HasProfileImage));
            return;
        }

        ProfileImagePath = string.Empty;
        ProfileImageUrl = string.Empty;
        OnPropertyChanged(nameof(ProfileImageSource));
        OnPropertyChanged(nameof(HasProfileImage));
    }

    private async Task LoadProfileCompletionAsync()
    {
        try
        {
            var educationTask = _apiService.GetEducationAsync();
            var experienceTask = _apiService.GetExperienceAsync();

            await Task.WhenAll(educationTask, experienceTask);

            var educationCount = educationTask.Result.Count;
            var experienceCount = experienceTask.Result.Count;

            EducationEntryCount = educationCount;
            ExperienceEntryCount = experienceCount;

            EducationCompletionProgress = CalculateCompletionProgress(educationCount, targetCount: 2);
            ExperienceCompletionProgress = CalculateCompletionProgress(experienceCount, targetCount: 2);

            EducationCompletionText = ToPercentageText(EducationCompletionProgress);
            ExperienceCompletionText = ToPercentageText(ExperienceCompletionProgress);
        }
        catch
        {
            EducationEntryCount = 0;
            ExperienceEntryCount = 0;
            EducationCompletionProgress = 0;
            ExperienceCompletionProgress = 0;
            EducationCompletionText = "0%";
            ExperienceCompletionText = "0%";
        }
    }

    private static double CalculateCompletionProgress(int currentCount, int targetCount)
    {
        if (targetCount <= 0)
        {
            return 0;
        }

        return Math.Min(currentCount / (double)targetCount, 1.0);
    }

    private static string ToPercentageText(double progress)
        => $"{(int)Math.Round(progress * 100)}%";

    partial void OnProfileImagePathChanged(string value)
    {
        OnPropertyChanged(nameof(ProfileImageSource));
        OnPropertyChanged(nameof(HasProfileImage));
    }

    partial void OnProfileImageUrlChanged(string value)
    {
        OnPropertyChanged(nameof(ProfileImageSource));
        OnPropertyChanged(nameof(HasProfileImage));
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
            {
                await Shell.Current.DisplayAlert("Profile image", $"Profile image could not be updated.\n{ex.Message}", "OK");
            }
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
    private async Task GoToGenerateResume() =>
        await Shell.Current.GoToAsync(nameof(GenerateResumePage));

    [RelayCommand]
    private async Task GoToProfile() =>
        await Shell.Current.GoToAsync("//main/profile");

    [RelayCommand]
    private async Task GoToResumeList() =>
        await Shell.Current.GoToAsync("//main/resume");
}
