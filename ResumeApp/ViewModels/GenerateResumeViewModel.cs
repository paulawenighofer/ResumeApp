using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ResumeApp.Services;
using ResumeApp.Views;
using Shared.DTO;
using Shared.Models;

namespace ResumeApp.ViewModels;

public partial class GenerateResumeViewModel : ObservableObject
{
    private readonly IApiService _apiService;

    [ObservableProperty] private string jobTitle = string.Empty;
    [ObservableProperty] private string targetCompany = string.Empty;
    [ObservableProperty] private string jobDescription = string.Empty;
    [ObservableProperty] private string personalSummary = string.Empty;
    [ObservableProperty] private bool includeEducation = true;
    [ObservableProperty] private bool includeExperience = true;
    [ObservableProperty] private bool includeSkills = true;
    [ObservableProperty] private bool includeProjects = true;
    [ObservableProperty] private bool includeCertifications = true;
    [ObservableProperty] private bool isBusy;
    [ObservableProperty] private string statusMessage = string.Empty;
    [ObservableProperty] private bool hasStatus;

    public GenerateResumeViewModel(IApiService apiService)
    {
        _apiService = apiService;
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

        if (string.IsNullOrWhiteSpace(JobDescription))
        {
            StatusMessage = "Please enter the job description.";
            HasStatus = true;
            return;
        }

        IsBusy = true;
        HasStatus = true;
        StatusMessage = "Generating draft...";

        try
        {
            var request = new CreateResumeDraftRequest
            {
                JobTitle = JobTitle.Trim(),
                TargetCompany = string.IsNullOrWhiteSpace(TargetCompany) ? "Not specified" : TargetCompany.Trim(),
                JobDescription = JobDescription.Trim(),
                ExperienceLevel = null,
                PersonalSummary = string.IsNullOrWhiteSpace(PersonalSummary) ? null : PersonalSummary.Trim(),
                IncludeEducation = IncludeEducation,
                IncludeExperience = IncludeExperience,
                IncludeSkills = IncludeSkills,
                IncludeProjects = IncludeProjects,
                IncludeCertifications = IncludeCertifications
            };

            var draft = await _apiService.CreateResumeDraftAsync(request);

            if (draft is null)
            {
                StatusMessage = "Failed to generate draft. Please try again.";
                HasStatus = true;
                return;
            }

            if (draft.Status == ResumeDraftStatus.Failed)
            {
                StatusMessage = string.IsNullOrWhiteSpace(draft.FailedReason)
                    ? "Draft generation failed."
                    : $"Draft failed: {draft.FailedReason}";
                HasStatus = true;
                return;
            }

            StatusMessage = draft.Status == ResumeDraftStatus.Pending
                ? "Draft is still generating."
                : "Draft is ready.";
            HasStatus = true;

            await Shell.Current.GoToAsync($"{nameof(ResumeDraftDetailPage)}?id={draft.Id}");
        }
        catch
        {
            StatusMessage = "Failed to generate draft. Please try again.";
            HasStatus = true;
        }
        finally
        {
            IsBusy = false;
        }
    }
}
