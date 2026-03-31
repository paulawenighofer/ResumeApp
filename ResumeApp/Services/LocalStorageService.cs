using System.Text.Json;
using ResumeApp.Models;

namespace ResumeApp.Services;

public class LocalStorageService : ILocalStorageService
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    private const string EducationKey = "draft_education";
    private const string ExperienceKey = "draft_experience";
    private const string SkillsKey = "draft_skills";
    private const string ProjectsKey = "draft_projects";
    private const string ProfileImageKey = "profile_image_path";

    public Task SaveEducationDraftAsync(List<EducationEntry> entries) => SaveAsync(EducationKey, entries);
    public Task<List<EducationEntry>> LoadEducationDraftAsync() => LoadAsync<EducationEntry>(EducationKey);
    public Task ClearEducationDraftAsync() => RemoveAsync(EducationKey);
    public Task SaveExperienceDraftAsync(List<ExperienceEntry> entries) => SaveAsync(ExperienceKey, entries);
    public Task<List<ExperienceEntry>> LoadExperienceDraftAsync() => LoadAsync<ExperienceEntry>(ExperienceKey);
    public Task ClearExperienceDraftAsync() => RemoveAsync(ExperienceKey);
    public Task SaveSkillsDraftAsync(List<SkillEntry> entries) => SaveAsync(SkillsKey, entries);
    public Task<List<SkillEntry>> LoadSkillsDraftAsync() => LoadAsync<SkillEntry>(SkillsKey);
    public Task ClearSkillsDraftAsync() => RemoveAsync(SkillsKey);
    public Task SaveProjectsDraftAsync(List<ProjectEntry> entries) => SaveAsync(ProjectsKey, entries);
    public Task<List<ProjectEntry>> LoadProjectsDraftAsync() => LoadAsync<ProjectEntry>(ProjectsKey);
    public Task ClearProjectsDraftAsync() => RemoveAsync(ProjectsKey);

    public Task SaveProfileImagePathAsync(string? imagePath)
    {
        if (string.IsNullOrWhiteSpace(imagePath))
        {
            Preferences.Default.Remove(ProfileImageKey);
        }
        else
        {
            Preferences.Default.Set(ProfileImageKey, imagePath);
        }

        return Task.CompletedTask;
    }

    public Task<string?> LoadProfileImagePathAsync()
        => Task.FromResult<string?>(Preferences.Default.ContainsKey(ProfileImageKey)
            ? Preferences.Default.Get(ProfileImageKey, string.Empty)
            : null);

    private static Task SaveAsync<T>(string key, List<T> items)
    {
        Preferences.Default.Set(key, JsonSerializer.Serialize(items, SerializerOptions));
        return Task.CompletedTask;
    }

    private static Task<List<T>> LoadAsync<T>(string key)
    {
        var json = Preferences.Default.ContainsKey(key)
            ? Preferences.Default.Get(key, string.Empty)
            : null;
        if (string.IsNullOrWhiteSpace(json))
        {
            return Task.FromResult(new List<T>());
        }

        try
        {
            return Task.FromResult(JsonSerializer.Deserialize<List<T>>(json, SerializerOptions) ?? new List<T>());
        }
        catch
        {
            Preferences.Default.Remove(key);
            return Task.FromResult(new List<T>());
        }
    }

    private static Task RemoveAsync(string key)
    {
        Preferences.Default.Remove(key);
        return Task.CompletedTask;
    }
}
