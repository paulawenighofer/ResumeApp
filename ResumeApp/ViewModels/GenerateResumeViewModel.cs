using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ResumeApp.Services;
using System.Text.Json;

namespace ResumeApp.ViewModels;

public partial class GenerateResumeViewModel : ObservableObject
{
    private readonly IPdfService _pdfService;

    public GenerateResumeViewModel(IPdfService pdfService)
    {
        _pdfService = pdfService;
    }

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

        try
        {
            if (!string.Equals(SelectedResumeFormat, "PDF", StringComparison.OrdinalIgnoreCase))
            {
                StatusMessage = "Only PDF export is wired up right now. Please choose PDF and try again.";
                HasStatus = true;
                return;
            }

            var payload = new
            {
                targetJob = new
                {
                    title = JobTitle.Trim(),
                    company = string.IsNullOrWhiteSpace(TargetCompany) ? null : TargetCompany.Trim(),
                    description = string.IsNullOrWhiteSpace(JobDescription) ? null : JobDescription.Trim(),
                    experienceLevel = SelectedExperienceLevel,
                    outputFormat = SelectedResumeFormat
                },
                content = new
                {
                    personalSummary = string.IsNullOrWhiteSpace(PersonalSummary)
                        ? $"Professional candidate targeting a {JobTitle.Trim()} role."
                        : PersonalSummary.Trim(),
                    sections = new
                    {
                        education = IncludeEducation,
                        experience = IncludeExperience,
                        skills = IncludeSkills,
                        projects = IncludeProjects
                    }
                },
                generatedAtUtc = DateTime.UtcNow
            };

            var json = JsonSerializer.Serialize(payload, new JsonSerializerOptions
            {
                WriteIndented = true
            });

            var export = await _pdfService.CreatePdfFromJsonAsync(json, BuildFileName());
            StatusMessage = $"PDF created successfully: {export.FileName}";
            HasStatus = true;
        }
        catch (JsonException)
        {
            StatusMessage = "The resume data could not be converted into valid JSON.";
            HasStatus = true;
        }
        catch (Exception ex)
        {
            StatusMessage = $"PDF export failed: {ex.Message}";
            HasStatus = true;
        }
        finally
        {
            IsBusy = false;
        }
    }

    private string BuildFileName()
    {
        var titlePart = string.IsNullOrWhiteSpace(JobTitle) ? "resume" : JobTitle.Trim().Replace(' ', '-');
        var companyPart = string.IsNullOrWhiteSpace(TargetCompany) ? string.Empty : $"-{TargetCompany.Trim().Replace(' ', '-')}";
        return $"{titlePart}{companyPart}-{DateTime.Now:yyyyMMdd-HHmmss}";
    }
}
