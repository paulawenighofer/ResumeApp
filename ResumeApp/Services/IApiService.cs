using ResumeApp.Models;

namespace ResumeApp.Services;

public interface IApiService
{
    Task<List<EducationEntry>> GetEducationAsync();
    Task<bool> PostEducationAsync(EducationEntry entry);
    Task<bool> UpdateEducationAsync(EducationEntry entry);
    Task<List<ExperienceEntry>> GetExperienceAsync();
    Task<bool> PostExperienceAsync(ExperienceEntry entry);
    Task<bool> UpdateExperienceAsync(ExperienceEntry entry);
    Task<List<SkillEntry>> GetSkillsAsync();
    Task<bool> PostSkillAsync(SkillEntry entry);
    Task<bool> UpdateSkillAsync(SkillEntry entry);
    Task<List<ProjectEntry>> GetProjectsAsync();
    Task<bool> PostProjectAsync(ProjectEntry entry);
    Task<bool> UpdateProjectAsync(ProjectEntry entry);
    Task<bool> UploadProjectImagesAsync(string projectId, IReadOnlyCollection<string> imagePaths);
    Task<string?> UploadProfileImageAsync(string imagePath);
}
