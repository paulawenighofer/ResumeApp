using ResumeApp.Models;

namespace ResumeApp.Services;

public interface ILocalStorageService
{
    Task SaveEducationDraftAsync(List<EducationEntry> entries);
    Task<List<EducationEntry>> LoadEducationDraftAsync();
    Task ClearEducationDraftAsync();
    Task SaveExperienceDraftAsync(List<ExperienceEntry> entries);
    Task<List<ExperienceEntry>> LoadExperienceDraftAsync();
    Task ClearExperienceDraftAsync();
    Task SaveSkillsDraftAsync(List<SkillEntry> entries);
    Task<List<SkillEntry>> LoadSkillsDraftAsync();
    Task ClearSkillsDraftAsync();
    Task SaveProjectsDraftAsync(List<ProjectEntry> entries);
    Task<List<ProjectEntry>> LoadProjectsDraftAsync();
    Task ClearProjectsDraftAsync();
    Task SaveCertificationsDraftAsync(List<CertificationEntry> entries);
    Task<List<CertificationEntry>> LoadCertificationsDraftAsync();
    Task ClearCertificationsDraftAsync();
    Task SaveProfileImagePathAsync(string? imagePath);
    Task<string?> LoadProfileImagePathAsync();
    Task ClearCurrentUserDataAsync();
    Task ClearAllLocalDataAsync();
}
