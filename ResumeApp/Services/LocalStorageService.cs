using System.Text.Json;
using ResumeApp.Models;

namespace ResumeApp.Services;

public class LocalStorageService : ILocalStorageService
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);
    private readonly CurrentUserService _currentUserService;

    private const string EducationKey = "draft_education";
    private const string ExperienceKey = "draft_experience";
    private const string SkillsKey = "draft_skills";
    private const string ProjectsKey = "draft_projects";
    private const string CertificationsKey = "draft_certifications";
    private const string ProfileImageKey = "profile_image_path";

    private static readonly string[] LegacyKeys =
    {
        EducationKey,
        ExperienceKey,
        SkillsKey,
        ProjectsKey,
        CertificationsKey,
        ProfileImageKey
    };

    public LocalStorageService(CurrentUserService currentUserService)
    {
        _currentUserService = currentUserService;
    }

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
    public Task SaveCertificationsDraftAsync(List<CertificationEntry> entries) => SaveAsync(CertificationsKey, entries);
    public Task<List<CertificationEntry>> LoadCertificationsDraftAsync() => LoadAsync<CertificationEntry>(CertificationsKey);
    public Task ClearCertificationsDraftAsync() => RemoveAsync(CertificationsKey);

    public async Task SaveProfileImagePathAsync(string? imagePath)
    {
        var key = await GetScopedKeyAsync(ProfileImageKey);
        if (string.IsNullOrWhiteSpace(imagePath))
        {
            Preferences.Default.Remove(key);
        }
        else
        {
            Preferences.Default.Set(key, imagePath);
        }

        Preferences.Default.Remove(ProfileImageKey);
    }

    public async Task<string?> LoadProfileImagePathAsync()
    {
        var key = await GetScopedKeyAsync(ProfileImageKey);
        if (Preferences.Default.ContainsKey(key))
        {
            return Preferences.Default.Get(key, string.Empty);
        }

        RemoveLegacyKey(ProfileImageKey);
        return null;
    }

    public async Task ClearCurrentUserDataAsync()
    {
        var currentUserId = await _currentUserService.GetCurrentUserIdAsync();
        if (string.IsNullOrWhiteSpace(currentUserId))
        {
            ClearLegacyKeys();
            return;
        }

        foreach (var key in LegacyKeys)
        {
            Preferences.Default.Remove(BuildScopedKey(currentUserId, key));
        }

        ClearLegacyKeys();
    }

    public Task ClearAllLocalDataAsync()
    {
        Preferences.Default.Clear();
        return Task.CompletedTask;
    }

    private async Task SaveAsync<T>(string key, List<T> items)
    {
        var scopedKey = await GetScopedKeyAsync(key);
        Preferences.Default.Set(scopedKey, JsonSerializer.Serialize(items, SerializerOptions));
        RemoveLegacyKey(key);
    }

    private async Task<List<T>> LoadAsync<T>(string key)
    {
        var scopedKey = await GetScopedKeyAsync(key);
        var json = Preferences.Default.ContainsKey(scopedKey)
            ? Preferences.Default.Get(scopedKey, string.Empty)
            : null;
        if (string.IsNullOrWhiteSpace(json))
        {
            RemoveLegacyKey(key);
            return new List<T>();
        }

        try
        {
            return JsonSerializer.Deserialize<List<T>>(json, SerializerOptions) ?? new List<T>();
        }
        catch
        {
            Preferences.Default.Remove(scopedKey);
            return new List<T>();
        }
    }

    private async Task RemoveAsync(string key)
    {
        var scopedKey = await GetScopedKeyAsync(key);
        Preferences.Default.Remove(scopedKey);
        RemoveLegacyKey(key);
    }

    private async Task<string> GetScopedKeyAsync(string baseKey)
    {
        var userId = await _currentUserService.GetCurrentUserIdAsync();
        return BuildScopedKey(userId, baseKey);
    }

    private static string BuildScopedKey(string? userId, string baseKey)
    {
        var scope = string.IsNullOrWhiteSpace(userId) ? "anonymous" : userId;
        return $"user:{scope}:{baseKey}";
    }

    private static void ClearLegacyKeys()
    {
        foreach (var key in LegacyKeys)
        {
            Preferences.Default.Remove(key);
        }
    }

    private static void RemoveLegacyKey(string key)
    {
        Preferences.Default.Remove(key);
    }
}
