using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ResumeApp.ViewModels;

public partial class GenerateResumeViewModel : ObservableObject
{
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

    public IList<string> ExperienceLevels { get; } =
        ["Entry-level", "Mid-level", "Senior", "Lead / Principal", "Executive"];

    public IList<string> ResumeFormats { get; } = ["PDF", "DOCX", "Plain Text"];

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

        await Task.Delay(1500); // Placeholder for API call

        IsBusy = false;
        StatusMessage = "Resume generation is coming soon! Your settings have been saved.";
        HasStatus = true;
    }
}
