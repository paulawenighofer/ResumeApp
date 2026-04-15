using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ResumeApp.Models;
using ResumeApp.Services;

namespace ResumeApp.ViewModels;

public partial class GenerateResumeViewModel : ObservableObject
{
    private readonly ILocalStorageService _localStorageService;
    private readonly ISyncCoordinator _syncCoordinator;

    [ObservableProperty] private string jobTitle = string.Empty;
    [ObservableProperty] private string targetCompany = string.Empty;
    [ObservableProperty] private string jobDescription = string.Empty;
    [ObservableProperty] private string selectedExperienceLevel = "Mid-level";
    [ObservableProperty] private string selectedResumeFormat = "PDF";
    [ObservableProperty] private string personalSummary = string.Empty;
    [ObservableProperty] private bool includeEducation = true;
    [ObservableProperty] private bool includeExperience = true;
    [ObservableProperty] private bool includeSkills = true;
    [ObservableProperty] private bool includeProjects = true;
    [ObservableProperty] private bool isBusy;
    [ObservableProperty] private string statusMessage = string.Empty;
    [ObservableProperty] private bool hasStatus;
    [ObservableProperty] private string selectedFilePath = string.Empty;

    public IList<string> ExperienceLevels { get; } =
        ["Entry-level", "Mid-level", "Senior", "Lead / Principal", "Executive"];

    public IList<string> ResumeFormats { get; } = ["PDF", "DOCX", "Plain Text"];

    public GenerateResumeViewModel(ILocalStorageService localStorageService, ISyncCoordinator syncCoordinator)
    {
        _localStorageService = localStorageService;
        _syncCoordinator = syncCoordinator;
    }

    [RelayCommand]
    private async Task PickResumeFile()
    {
        var result = await FilePicker.Default.PickAsync(new PickOptions
        {
            PickerTitle = "Select generated resume file"
        });

        if (result?.FullPath is not null)
        {
            SelectedFilePath = result.FullPath;
        }
    }

    [RelayCommand]
    private async Task Generate()
    {
        if (string.IsNullOrWhiteSpace(JobTitle))
        {
            StatusMessage = "Please enter the job title you are applying for.";
            HasStatus = true;
            return;
        }

        IsBusy = true;
        HasStatus = false;

        var entry = new StoredResumeEntry
        {
            Id = Guid.NewGuid().ToString(),
            TargetJobTitle = JobTitle.Trim(),
            TargetCompany = string.IsNullOrWhiteSpace(TargetCompany) ? null : TargetCompany.Trim(),
            JobDescription = JobDescription,
            CompanyDescription = $"Experience Level: {SelectedExperienceLevel}\nSummary: {PersonalSummary}".Trim(),
            GeneratedContent = BuildGeneratedContent(),
            LocalFilePath = string.IsNullOrWhiteSpace(SelectedFilePath) ? null : SelectedFilePath,
            CreatedAt = DateTime.UtcNow,
            ResumeUpdatedAt = DateTime.UtcNow
        };

        await _localStorageService.SaveItemAsync(entry);
        await _syncCoordinator.SyncResumesAsync();

        IsBusy = false;
        StatusMessage = "Resume draft saved locally and queued for sync.";
        HasStatus = true;
    }

    private string BuildGeneratedContent()
        => $"{{\"personalSummary\":\"{Escape(PersonalSummary)}\",\"includeEducation\":{IncludeEducation.ToString().ToLowerInvariant()},\"includeExperience\":{IncludeExperience.ToString().ToLowerInvariant()},\"includeSkills\":{IncludeSkills.ToString().ToLowerInvariant()},\"includeProjects\":{IncludeProjects.ToString().ToLowerInvariant()}}}";

    private static string Escape(string value) => value.Replace("\\", "\\\\").Replace("\"", "\\\"");
}
