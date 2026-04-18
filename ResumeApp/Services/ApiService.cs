using System.Net.Http.Headers;
using System.Net.Http.Json;
using ResumeApp.Models;
using Shared.DTO;
using Shared.Models;

namespace ResumeApp.Services;

public class ApiService : IApiService
{
    private readonly HttpClient _httpClient;

    public ApiService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<List<EducationEntry>> GetEducationAsync()
    {
        var response = await SendAsync(HttpMethod.Get, "api/educations");
        if (response is null || !response.IsSuccessStatusCode)
        {
            return [];
        }

        var items = await response.Content.ReadFromJsonAsync<List<Education>>();
        return items?.Select(MapEducation).ToList() ?? [];
    }

    public async Task<bool> PostEducationAsync(EducationEntry entry)
    {
        var payload = new Education
        {
            Institution = entry.School,
            Degree = entry.Degree,
            FieldOfStudy = entry.FieldOfStudy,
            StartDate = ToUtcDate(entry.StartDate),
            EndDate = ToUtcDate(entry.EndDate),
            GPA = decimal.TryParse(entry.GPA, out var gpa) ? gpa : null,
            Description = entry.Description
        };

        var response = await SendAsync(HttpMethod.Post, "api/educations", JsonContent.Create(payload));
        if (response is null || !response.IsSuccessStatusCode)
        {
            return false;
        }

        var education = await response.Content.ReadFromJsonAsync<Education>();
        if (education is not null)
        {
            entry.Id = education.Id.ToString();
        }

        return true;
    }

    public async Task<bool> UpdateEducationAsync(EducationEntry entry)
    {
        if (!int.TryParse(entry.Id, out var id))
        {
            return await PostEducationAsync(entry);
        }

        var payload = new Education
        {
            Institution = entry.School,
            Degree = entry.Degree,
            FieldOfStudy = entry.FieldOfStudy,
            StartDate = ToUtcDate(entry.StartDate),
            EndDate = ToUtcDate(entry.EndDate),
            GPA = decimal.TryParse(entry.GPA, out var gpa) ? gpa : null,
            Description = entry.Description
        };

        var updated = await SendJsonAsync(HttpMethod.Put, $"api/educations/{id}", payload);
        return updated || await PostEducationAsync(entry);
    }

    public async Task<bool> DeleteEducationAsync(string id)
    {
        if (!int.TryParse(id, out var educationId))
        {
            return true;
        }

        var response = await SendAsync(HttpMethod.Delete, $"api/educations/{educationId}");
        return response?.IsSuccessStatusCode == true;
    }

    public async Task<List<ExperienceEntry>> GetExperienceAsync()
    {
        var response = await SendAsync(HttpMethod.Get, "api/experiences");
        if (response is null || !response.IsSuccessStatusCode)
        {
            return [];
        }

        var items = await response.Content.ReadFromJsonAsync<List<Experience>>();
        return items?.Select(MapExperience).ToList() ?? [];
    }

    public async Task<bool> PostExperienceAsync(ExperienceEntry entry)
    {
        var response = await SendAsync(HttpMethod.Post, "api/experiences", JsonContent.Create(new Experience
        {
            Company = entry.Company,
            JobTitle = entry.JobTitle,
            Location = entry.Location,
            StartDate = entry.StartDate,
            EndDate = entry.IsCurrentJob ? null : entry.EndDate,
            IsCurrentJob = entry.IsCurrentJob,
            Responsibilities = entry.Description
        }));

        if (response is null || !response.IsSuccessStatusCode)
        {
            return false;
        }

        var experience = await response.Content.ReadFromJsonAsync<Experience>();
        if (experience is not null)
        {
            entry.Id = experience.Id.ToString();
        }

        return true;
    }

    public async Task<bool> UpdateExperienceAsync(ExperienceEntry entry)
    {
        if (!int.TryParse(entry.Id, out var id))
        {
            return await PostExperienceAsync(entry);
        }

        var payload = new Experience
        {
            Company = entry.Company,
            JobTitle = entry.JobTitle,
            Location = entry.Location,
            StartDate = entry.StartDate,
            EndDate = entry.IsCurrentJob ? null : entry.EndDate,
            IsCurrentJob = entry.IsCurrentJob,
            Responsibilities = entry.Description
        };

        var updated = await SendJsonAsync(HttpMethod.Put, $"api/experiences/{id}", payload);
        return updated || await PostExperienceAsync(entry);
    }

    public async Task<bool> DeleteExperienceAsync(string id)
    {
        if (!int.TryParse(id, out var experienceId))
        {
            return true;
        }

        var response = await SendAsync(HttpMethod.Delete, $"api/experiences/{experienceId}");
        return response?.IsSuccessStatusCode == true;
    }

    public async Task<List<SkillEntry>> GetSkillsAsync()
    {
        var response = await SendAsync(HttpMethod.Get, "api/skills");
        if (response is null || !response.IsSuccessStatusCode)
        {
            return [];
        }

        var items = await response.Content.ReadFromJsonAsync<List<Skill>>();
        return items?.Select(MapSkill).ToList() ?? [];
    }

    public async Task<bool> PostSkillAsync(SkillEntry entry)
    {
        var response = await SendAsync(HttpMethod.Post, "api/skills", JsonContent.Create(new Skill
        {
            Name = entry.Name,
            Category = entry.Category,
            ProficiencyLevel = entry.ProficiencyScore
        }));

        if (response is null || !response.IsSuccessStatusCode)
        {
            return false;
        }

        var skill = await response.Content.ReadFromJsonAsync<Skill>();
        if (skill is not null)
        {
            entry.Id = skill.Id.ToString();
        }

        return true;
    }

    public async Task<bool> UpdateSkillAsync(SkillEntry entry)
    {
        if (!int.TryParse(entry.Id, out var id))
        {
            return await PostSkillAsync(entry);
        }

        var payload = new Skill
        {
            Name = entry.Name,
            Category = entry.Category,
            ProficiencyLevel = entry.ProficiencyScore
        };

        var updated = await SendJsonAsync(HttpMethod.Put, $"api/skills/{id}", payload);
        return updated || await PostSkillAsync(entry);
    }

    public async Task<bool> DeleteSkillAsync(string id)
    {
        if (!int.TryParse(id, out var skillId))
        {
            return true;
        }

        var response = await SendAsync(HttpMethod.Delete, $"api/skills/{skillId}");
        return response?.IsSuccessStatusCode == true;
    }

    public async Task<List<ProjectEntry>> GetProjectsAsync()
    {
        var response = await SendAsync(HttpMethod.Get, "api/projects");
        if (response is null || !response.IsSuccessStatusCode)
        {
            return [];
        }

        var items = await response.Content.ReadFromJsonAsync<List<ResumeProject>>();
        return items?.Select(MapProject).ToList() ?? [];
    }

    public async Task<bool> PostProjectAsync(ProjectEntry entry)
    {
        var response = await SendAsync(HttpMethod.Post, "api/projects", JsonContent.Create(new ResumeProject
        {
            Name = entry.Name,
            Description = entry.Description,
            Technologies = entry.Technologies,
            Url = entry.ProjectUrl,
            StartDate = DateOnly.FromDateTime(entry.StartDate),
            EndDate = DateOnly.FromDateTime(entry.EndDate)
        }));

        if (response is null || !response.IsSuccessStatusCode)
        {
            return false;
        }

        var project = await response.Content.ReadFromJsonAsync<ResumeProject>();
        if (project is not null)
        {
            entry.Id = project.Id.ToString();
        }

        return true;
    }

    public async Task<bool> UpdateProjectAsync(ProjectEntry entry)
    {
        if (!int.TryParse(entry.Id, out var id))
        {
            return await PostProjectAsync(entry);
        }

        var payload = new ResumeProject
        {
            Name = entry.Name,
            Description = entry.Description,
            Technologies = entry.Technologies,
            Url = entry.ProjectUrl,
            StartDate = DateOnly.FromDateTime(entry.StartDate),
            EndDate = DateOnly.FromDateTime(entry.EndDate)
        };

        var response = await SendAsync(HttpMethod.Put, $"api/projects/{id}", JsonContent.Create(payload));

        return response?.IsSuccessStatusCode == true || await PostProjectAsync(entry);
    }

    public async Task<bool> DeleteProjectAsync(string id)
    {
        if (!int.TryParse(id, out var projectId))
        {
            return true;
        }

        var response = await SendAsync(HttpMethod.Delete, $"api/projects/{projectId}");
        return response?.IsSuccessStatusCode == true;
    }

    public async Task<List<CertificationEntry>> GetCertificationsAsync()
    {
        var response = await SendAsync(HttpMethod.Get, "api/certifications");
        if (response is null || !response.IsSuccessStatusCode)
        {
            return [];
        }

        var items = await response.Content.ReadFromJsonAsync<List<Certification>>();
        return items?.Select(MapCertification).ToList() ?? [];
    }

    public async Task<bool> PostCertificationAsync(CertificationEntry entry)
    {
        var response = await SendAsync(HttpMethod.Post, "api/certifications", JsonContent.Create(new Certification
        {
            Name = entry.Name,
            IssuingOrganization = entry.IssuingOrganization,
            IssueDate = ToUtcDate(entry.IssueDate),
            ExpirationDate = ToUtcDate(entry.ExpirationDate),
            CredentialId = entry.CredentialId,
            CredentialUrl = entry.CredentialUrl
        }));

        if (response is null || !response.IsSuccessStatusCode)
        {
            return false;
        }

        var certification = await response.Content.ReadFromJsonAsync<Certification>();
        if (certification is not null)
        {
            entry.Id = certification.Id.ToString();
        }

        return true;
    }

    public async Task<bool> UpdateCertificationAsync(CertificationEntry entry)
    {
        if (!int.TryParse(entry.Id, out var id))
        {
            return await PostCertificationAsync(entry);
        }

        var payload = new Certification
        {
            Name = entry.Name,
            IssuingOrganization = entry.IssuingOrganization,
            IssueDate = ToUtcDate(entry.IssueDate),
            ExpirationDate = ToUtcDate(entry.ExpirationDate),
            CredentialId = entry.CredentialId,
            CredentialUrl = entry.CredentialUrl
        };

        var updated = await SendJsonAsync(HttpMethod.Put, $"api/certifications/{id}", payload);
        return updated || await PostCertificationAsync(entry);
    }

    public async Task<bool> DeleteCertificationAsync(string id)
    {
        if (!int.TryParse(id, out var certificationId))
        {
            return true;
        }

        var response = await SendAsync(HttpMethod.Delete, $"api/certifications/{certificationId}");
        return response?.IsSuccessStatusCode == true;
    }

    public async Task<string?> UploadProfileImageAsync(string imagePath)
    {
        var content = BuildMultipartContent([imagePath], "file");
        var response = await SendAsync(HttpMethod.Post, "api/profile/image", content);
        if (response is null || !response.IsSuccessStatusCode)
        {
            return null;
        }

        var result = await response.Content.ReadFromJsonAsync<ImageUploadResponse>();
        return result?.ImageUrl;
    }

    public async Task<ResumeDraftResponse?> CreateResumeDraftAsync(CreateResumeDraftRequest request)
    {
        var response = await SendAsync(HttpMethod.Post, "api/resumes/drafts", JsonContent.Create(request));
        if (response is null || !response.IsSuccessStatusCode)
        {
            return null;
        }

        return await response.Content.ReadFromJsonAsync<ResumeDraftResponse>();
    }

    public async Task<List<ResumeListItemDto>> GetResumeDraftsAsync()
    {
        var response = await SendAsync(HttpMethod.Get, "api/resumes");
        if (response is null || !response.IsSuccessStatusCode)
        {
            return [];
        }

        var items = await response.Content.ReadFromJsonAsync<List<ResumeListItemDto>>();
        return items ?? [];
    }

    public async Task<ResumeDetailDto?> GetResumeDraftAsync(int id)
    {
        var response = await SendAsync(HttpMethod.Get, $"api/resumes/{id}");
        if (response is null || !response.IsSuccessStatusCode)
        {
            return null;
        }

        return await response.Content.ReadFromJsonAsync<ResumeDetailDto>();
    }

    private async Task<bool> SendJsonAsync(HttpMethod method, string url, object payload)
    {
        var response = await SendAsync(method, url, JsonContent.Create(payload));
        return response?.IsSuccessStatusCode == true;
    }

    private async Task<HttpResponseMessage?> SendAsync(HttpMethod method, string url, HttpContent? content = null)
    {
        var request = new HttpRequestMessage(method, url)
        {
            Content = content
        };

        var token = await SecureStorage.GetAsync("auth_token");
        if (!string.IsNullOrWhiteSpace(token))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        try
        {
            return await _httpClient.SendAsync(request);
        }
        catch
        {
            return null;
        }
    }

    private static MultipartFormDataContent BuildMultipartContent(IEnumerable<string> filePaths, string fieldName)
    {
        var content = new MultipartFormDataContent();

        foreach (var filePath in filePaths.Where(File.Exists))
        {
            var fileContent = new StreamContent(File.OpenRead(filePath));
            fileContent.Headers.ContentType = new MediaTypeHeaderValue(GetMimeType(filePath));
            content.Add(fileContent, fieldName, Path.GetFileName(filePath));
        }

        return content;
    }

    private static string GetMimeType(string filePath) => Path.GetExtension(filePath).ToLowerInvariant() switch
    {
        ".png" => "image/png",
        ".gif" => "image/gif",
        ".webp" => "image/webp",
        _ => "image/jpeg"
    };

    private static DateTime ToUtcDate(DateTime value)
        => DateTime.SpecifyKind(value.Date, DateTimeKind.Utc);

    private static EducationEntry MapEducation(Education education) => new()
    {
        Id = education.Id.ToString(),
        School = education.Institution,
        Degree = education.Degree,
        FieldOfStudy = education.FieldOfStudy ?? string.Empty,
        StartDate = education.StartDate,
        EndDate = education.EndDate ?? education.StartDate,
        GPA = education.GPA?.ToString(),
        Description = education.Description
    };

    private static ExperienceEntry MapExperience(Experience experience) => new()
    {
        Id = experience.Id.ToString(),
        Company = experience.Company,
        JobTitle = experience.JobTitle,
        Location = experience.Location ?? string.Empty,
        StartDate = experience.StartDate,
        EndDate = experience.EndDate ?? DateTime.Now,
        IsCurrentJob = experience.IsCurrentJob,
        Description = experience.Responsibilities ?? string.Empty
    };

    private static SkillEntry MapSkill(Skill skill) => new()
    {
        Id = skill.Id.ToString(),
        Name = skill.Name,
        Category = skill.Category ?? "Other",
        ProficiencyLevel = skill.ProficiencyLevel switch
        {
            1 => "Beginner",
            2 => "Intermediate",
            3 => "Advanced",
            4 => "Expert",
            _ => "Intermediate"
        }
    };

    private static ProjectEntry MapProject(ResumeProject project) => new()
    {
        Id = project.Id.ToString(),
        Name = project.Name,
        Description = project.Description ?? string.Empty,
        Technologies = project.Technologies,
        ProjectUrl = project.Url,
        StartDate = project.StartDate?.ToDateTime(TimeOnly.MinValue) ?? DateTime.Now,
        EndDate = project.EndDate?.ToDateTime(TimeOnly.MinValue) ?? DateTime.Now
    };

    private static CertificationEntry MapCertification(Certification certification) => new()
    {
        Id = certification.Id.ToString(),
        Name = certification.Name,
        IssuingOrganization = certification.IssuingOrganization,
        IssueDate = certification.IssueDate ?? DateTime.Now,
        ExpirationDate = certification.ExpirationDate ?? DateTime.Now.AddYears(1),
        CredentialId = certification.CredentialId,
        CredentialUrl = certification.CredentialUrl
    };

    private sealed class ImageUploadResponse
    {
        public string? ImageUrl { get; set; }
    }
}
