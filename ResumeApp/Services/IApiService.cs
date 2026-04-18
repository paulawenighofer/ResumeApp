using ResumeApp.Models;
using Shared.DTO;

namespace ResumeApp.Services;

public interface IApiService
{
    Task<List<EducationEntry>> GetEducationAsync();
    Task<bool> PostEducationAsync(EducationEntry entry);
    Task<bool> UpdateEducationAsync(EducationEntry entry);
    Task<bool> DeleteEducationAsync(string id);
    Task<List<ExperienceEntry>> GetExperienceAsync();
    Task<bool> PostExperienceAsync(ExperienceEntry entry);
    Task<bool> UpdateExperienceAsync(ExperienceEntry entry);
    Task<bool> DeleteExperienceAsync(string id);
    Task<List<SkillEntry>> GetSkillsAsync();
    Task<bool> PostSkillAsync(SkillEntry entry);
    Task<bool> UpdateSkillAsync(SkillEntry entry);
    Task<bool> DeleteSkillAsync(string id);
    Task<List<ProjectEntry>> GetProjectsAsync();
    Task<bool> PostProjectAsync(ProjectEntry entry);
    Task<bool> UpdateProjectAsync(ProjectEntry entry);
    Task<bool> DeleteProjectAsync(string id);
    Task<List<CertificationEntry>> GetCertificationsAsync();
    Task<bool> PostCertificationAsync(CertificationEntry entry);
    Task<bool> UpdateCertificationAsync(CertificationEntry entry);
    Task<bool> DeleteCertificationAsync(string id);
    Task<ResumeDraftResponse?> CreateResumeDraftAsync(CreateResumeDraftRequest request);
    Task<List<ResumeListItemDto>> GetResumeDraftsAsync();
    Task<ResumeDetailDto?> GetResumeDraftAsync(int id);
    Task<ResumeDetailDto?> SaveResumeDraftEditAsync(int id, SaveDraftEditRequest request);
    Task<ApproveDraftResponse?> ApproveResumeDraftAsync(int id, ApproveDraftRequest request);
    Task<ResumeDetailDto?> GenerateResumePdfAsync(int id);
    Task<byte[]?> DownloadResumePdfAsync(int id);
    Task<string?> UploadProfileImageAsync(string imagePath);
}
