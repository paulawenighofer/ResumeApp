using ResumeApp.Models;
using ResumeApp.Services;
using ResumeApp.ViewModels;

namespace MauiTests;

public class MainPageViewModelTests
{
    [Fact]
    public void Constructor_SetsExpectedDefaults()
    {
        var viewModel = new MainPageViewModel(
            TestAuthServiceFactory.Create(),
            new StubApiService(),
            new StubLocalStorageService());

        Assert.Equal("AI Resume Builder", viewModel.AppHeading);
        Assert.Equal("User", viewModel.UserName);
        Assert.Equal(string.Empty, viewModel.UserEmail);
    }

    private sealed class StubApiService : IApiService
    {
        public Task<List<EducationEntry>> GetEducationAsync() => Task.FromResult(new List<EducationEntry>());
        public Task<bool> PostEducationAsync(EducationEntry entry) => Task.FromResult(true);
        public Task<bool> UpdateEducationAsync(EducationEntry entry) => Task.FromResult(true);
        public Task<List<ExperienceEntry>> GetExperienceAsync() => Task.FromResult(new List<ExperienceEntry>());
        public Task<bool> PostExperienceAsync(ExperienceEntry entry) => Task.FromResult(true);
        public Task<bool> UpdateExperienceAsync(ExperienceEntry entry) => Task.FromResult(true);
        public Task<List<SkillEntry>> GetSkillsAsync() => Task.FromResult(new List<SkillEntry>());
        public Task<bool> PostSkillAsync(SkillEntry entry) => Task.FromResult(true);
        public Task<bool> UpdateSkillAsync(SkillEntry entry) => Task.FromResult(true);
        public Task<List<ProjectEntry>> GetProjectsAsync() => Task.FromResult(new List<ProjectEntry>());
        public Task<bool> PostProjectAsync(ProjectEntry entry) => Task.FromResult(true);
        public Task<bool> UpdateProjectAsync(ProjectEntry entry) => Task.FromResult(true);
        public Task<bool> UploadProjectImagesAsync(string projectId, IReadOnlyCollection<string> imagePaths) => Task.FromResult(true);
        public Task<string?> UploadProfileImageAsync(string imagePath) => Task.FromResult<string?>(null);
    }

    private sealed class StubLocalStorageService : ILocalStorageService
    {
        public Task SaveEducationDraftAsync(List<EducationEntry> entries) => Task.CompletedTask;
        public Task<List<EducationEntry>> LoadEducationDraftAsync() => Task.FromResult(new List<EducationEntry>());
        public Task ClearEducationDraftAsync() => Task.CompletedTask;
        public Task SaveExperienceDraftAsync(List<ExperienceEntry> entries) => Task.CompletedTask;
        public Task<List<ExperienceEntry>> LoadExperienceDraftAsync() => Task.FromResult(new List<ExperienceEntry>());
        public Task ClearExperienceDraftAsync() => Task.CompletedTask;
        public Task SaveSkillsDraftAsync(List<SkillEntry> entries) => Task.CompletedTask;
        public Task<List<SkillEntry>> LoadSkillsDraftAsync() => Task.FromResult(new List<SkillEntry>());
        public Task ClearSkillsDraftAsync() => Task.CompletedTask;
        public Task SaveProjectsDraftAsync(List<ProjectEntry> entries) => Task.CompletedTask;
        public Task<List<ProjectEntry>> LoadProjectsDraftAsync() => Task.FromResult(new List<ProjectEntry>());
        public Task ClearProjectsDraftAsync() => Task.CompletedTask;
        public Task SaveProfileImagePathAsync(string? imagePath) => Task.CompletedTask;
        public Task<string?> LoadProfileImagePathAsync() => Task.FromResult<string?>(null);
    }
}
