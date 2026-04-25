using Microsoft.AspNetCore.Components.Authorization;
using Shared.DTO;
using Shared.Models;
using System.Net.Http.Headers;
using System.Security.Claims;

namespace ResumeApp.Web.Services;

public class ApiClient
{
    private readonly HttpClient _http;
    private readonly AuthenticationStateProvider _authProvider;

    public ApiClient(HttpClient http, AuthenticationStateProvider authProvider)
    {
        _http = http;
        _authProvider = authProvider;
    }

    private async Task AuthorizeAsync()
    {
        var state = await _authProvider.GetAuthenticationStateAsync();
        var token = state.User.FindFirstValue("jwt");
        _http.DefaultRequestHeaders.Authorization = token is not null
            ? new AuthenticationHeaderValue("Bearer", token)
            : null;
    }

    // ── Auth ──────────────────────────────────────────────────────────────

    public async Task<AuthResponseDto?> LoginAsync(LoginDto dto)
    {
        var res = await _http.PostAsJsonAsync("/api/auth/login", dto);
        if (!res.IsSuccessStatusCode) return null;
        return await res.Content.ReadFromJsonAsync<AuthResponseDto>();
    }

    public async Task<(bool Success, string? Message)> RegisterAsync(RegisterDto dto)
    {
        var res = await _http.PostAsJsonAsync("/api/auth/register", dto);
        if (res.IsSuccessStatusCode)
        {
            var data = await res.Content.ReadFromJsonAsync<RegisterPendingResponseDto>();
            return (true, data?.Message);
        }
        var error = await TryReadErrorAsync(res);
        return (false, error);
    }

    public async Task<AuthResponseDto?> VerifyOtpAsync(VerifyOtpDto dto)
    {
        var res = await _http.PostAsJsonAsync("/api/auth/verify-otp", dto);
        if (!res.IsSuccessStatusCode) return null;
        return await res.Content.ReadFromJsonAsync<AuthResponseDto>();
    }

    public async Task<bool> ResendOtpAsync(string email)
    {
        var res = await _http.PostAsJsonAsync("/api/auth/resend-otp", new ResendOtpDto { Email = email });
        return res.IsSuccessStatusCode;
    }

    public async Task<bool> ForgotPasswordAsync(string email)
    {
        var res = await _http.PostAsJsonAsync("/api/auth/forgot-password", new ForgotPasswordDto { Email = email });
        return res.IsSuccessStatusCode;
    }

    public async Task<(bool Success, string? Error)> ResetPasswordAsync(ResetPasswordDto dto)
    {
        var res = await _http.PostAsJsonAsync("/api/auth/reset-password", dto);
        if (res.IsSuccessStatusCode) return (true, null);
        var error = await TryReadErrorAsync(res);
        return (false, error);
    }

    public async Task<AuthResponseDto?> GetMeAsync()
    {
        await AuthorizeAsync();
        try { return await _http.GetFromJsonAsync<AuthResponseDto>("/api/auth/me"); }
        catch { return null; }
    }

    // ── Profile image ─────────────────────────────────────────────────────

    public async Task<string?> UploadProfileImageAsync(Stream stream, string fileName)
    {
        await AuthorizeAsync();
        using var content = new MultipartFormDataContent();
        content.Add(new StreamContent(stream), "file", fileName);
        var res = await _http.PostAsync("/api/profile/image", content);
        if (!res.IsSuccessStatusCode) return null;
        var result = await res.Content.ReadFromJsonAsync<ProfileImageUploadResponse>();
        return result?.Url;
    }

    // ── Education ─────────────────────────────────────────────────────────

    public async Task<List<Education>> GetEducationsAsync()
    {
        await AuthorizeAsync();
        return await _http.GetFromJsonAsync<List<Education>>("/api/educations") ?? [];
    }

    public async Task<Education?> CreateEducationAsync(Education item)
    {
        await AuthorizeAsync();
        var res = await _http.PostAsJsonAsync("/api/educations", item);
        if (!res.IsSuccessStatusCode) return null;
        return await res.Content.ReadFromJsonAsync<Education>();
    }

    public async Task<bool> UpdateEducationAsync(int id, Education item)
    {
        await AuthorizeAsync();
        var res = await _http.PutAsJsonAsync($"/api/educations/{id}", item);
        return res.IsSuccessStatusCode;
    }

    public async Task<bool> DeleteEducationAsync(int id)
    {
        await AuthorizeAsync();
        var res = await _http.DeleteAsync($"/api/educations/{id}");
        return res.IsSuccessStatusCode;
    }

    // ── Experience ────────────────────────────────────────────────────────

    public async Task<List<Experience>> GetExperiencesAsync()
    {
        await AuthorizeAsync();
        return await _http.GetFromJsonAsync<List<Experience>>("/api/experiences") ?? [];
    }

    public async Task<Experience?> CreateExperienceAsync(Experience item)
    {
        await AuthorizeAsync();
        var res = await _http.PostAsJsonAsync("/api/experiences", item);
        if (!res.IsSuccessStatusCode) return null;
        return await res.Content.ReadFromJsonAsync<Experience>();
    }

    public async Task<bool> UpdateExperienceAsync(int id, Experience item)
    {
        await AuthorizeAsync();
        var res = await _http.PutAsJsonAsync($"/api/experiences/{id}", item);
        return res.IsSuccessStatusCode;
    }

    public async Task<bool> DeleteExperienceAsync(int id)
    {
        await AuthorizeAsync();
        var res = await _http.DeleteAsync($"/api/experiences/{id}");
        return res.IsSuccessStatusCode;
    }

    // ── Skills ────────────────────────────────────────────────────────────

    public async Task<List<Skill>> GetSkillsAsync()
    {
        await AuthorizeAsync();
        return await _http.GetFromJsonAsync<List<Skill>>("/api/skills") ?? [];
    }

    public async Task<Skill?> CreateSkillAsync(Skill item)
    {
        await AuthorizeAsync();
        var res = await _http.PostAsJsonAsync("/api/skills", item);
        if (!res.IsSuccessStatusCode) return null;
        return await res.Content.ReadFromJsonAsync<Skill>();
    }

    public async Task<bool> UpdateSkillAsync(int id, Skill item)
    {
        await AuthorizeAsync();
        var res = await _http.PutAsJsonAsync($"/api/skills/{id}", item);
        return res.IsSuccessStatusCode;
    }

    public async Task<bool> DeleteSkillAsync(int id)
    {
        await AuthorizeAsync();
        var res = await _http.DeleteAsync($"/api/skills/{id}");
        return res.IsSuccessStatusCode;
    }

    // ── Projects ──────────────────────────────────────────────────────────

    public async Task<List<Project>> GetProjectsAsync()
    {
        await AuthorizeAsync();
        return await _http.GetFromJsonAsync<List<Project>>("/api/projects") ?? [];
    }

    public async Task<Project?> CreateProjectAsync(Project item)
    {
        await AuthorizeAsync();
        var res = await _http.PostAsJsonAsync("/api/projects", item);
        if (!res.IsSuccessStatusCode) return null;
        return await res.Content.ReadFromJsonAsync<Project>();
    }

    public async Task<bool> UpdateProjectAsync(int id, Project item)
    {
        await AuthorizeAsync();
        var res = await _http.PutAsJsonAsync($"/api/projects/{id}", item);
        return res.IsSuccessStatusCode;
    }

    public async Task<bool> DeleteProjectAsync(int id)
    {
        await AuthorizeAsync();
        var res = await _http.DeleteAsync($"/api/projects/{id}");
        return res.IsSuccessStatusCode;
    }

    // ── Certifications ────────────────────────────────────────────────────

    public async Task<List<Certification>> GetCertificationsAsync()
    {
        await AuthorizeAsync();
        return await _http.GetFromJsonAsync<List<Certification>>("/api/certifications") ?? [];
    }

    public async Task<Certification?> CreateCertificationAsync(Certification item)
    {
        await AuthorizeAsync();
        var res = await _http.PostAsJsonAsync("/api/certifications", item);
        if (!res.IsSuccessStatusCode) return null;
        return await res.Content.ReadFromJsonAsync<Certification>();
    }

    public async Task<bool> UpdateCertificationAsync(int id, Certification item)
    {
        await AuthorizeAsync();
        var res = await _http.PutAsJsonAsync($"/api/certifications/{id}", item);
        return res.IsSuccessStatusCode;
    }

    public async Task<bool> DeleteCertificationAsync(int id)
    {
        await AuthorizeAsync();
        var res = await _http.DeleteAsync($"/api/certifications/{id}");
        return res.IsSuccessStatusCode;
    }

    // ── Resumes ───────────────────────────────────────────────────────────

    public async Task<List<ResumeListItemDto>> GetResumesAsync()
    {
        await AuthorizeAsync();
        return await _http.GetFromJsonAsync<List<ResumeListItemDto>>("/api/resumes") ?? [];
    }

    public async Task<ResumeDetailDto?> GetResumeAsync(int id)
    {
        await AuthorizeAsync();
        try { return await _http.GetFromJsonAsync<ResumeDetailDto>($"/api/resumes/{id}"); }
        catch { return null; }
    }

    public async Task<ResumeDraftResponse?> CreateDraftAsync(CreateResumeDraftRequest req)
    {
        await AuthorizeAsync();
        var res = await _http.PostAsJsonAsync("/api/resumes/drafts", req);
        if (!res.IsSuccessStatusCode) return null;
        return await res.Content.ReadFromJsonAsync<ResumeDraftResponse>();
    }

    public async Task<bool> SaveDraftEditAsync(int id, string editedJson)
    {
        await AuthorizeAsync();
        var res = await _http.PutAsJsonAsync($"/api/resumes/{id}/draft", new SaveDraftEditRequest { EditedResumeJson = editedJson });
        return res.IsSuccessStatusCode;
    }

    public async Task<ApproveDraftResponse?> ApproveDraftAsync(int id, string finalJson)
    {
        await AuthorizeAsync();
        var res = await _http.PostAsJsonAsync($"/api/resumes/{id}/approve", new ApproveDraftRequest { FinalResumeJson = finalJson });
        if (!res.IsSuccessStatusCode) return null;
        return await res.Content.ReadFromJsonAsync<ApproveDraftResponse>();
    }

    public async Task<bool> GeneratePdfAsync(int id)
    {
        await AuthorizeAsync();
        var res = await _http.PostAsync($"/api/resumes/{id}/generate-pdf", null);
        return res.IsSuccessStatusCode;
    }

    // ── Helpers ───────────────────────────────────────────────────────────

    private static async Task<string?> TryReadErrorAsync(HttpResponseMessage res)
    {
        try
        {
            var body = await res.Content.ReadAsStringAsync();
            return string.IsNullOrWhiteSpace(body) ? res.ReasonPhrase : body;
        }
        catch
        {
            return res.ReasonPhrase;
        }
    }
}

public record ProfileImageUploadResponse(string Url);
