using System.Text.Json;
using API.Data;
using Microsoft.EntityFrameworkCore;
using Shared.DTO;

namespace API.Services;

public class ResumeProfileAssembler : IResumeProfileAssembler
{
    private readonly AppDbContext _db;

    public ResumeProfileAssembler(AppDbContext db)
    {
        _db = db;
    }

    public async Task<ResumeGenerationPayload> AssembleAsync(string userId, CreateResumeDraftRequest request, CancellationToken cancellationToken = default)
    {
        var user = await _db.Users
            .AsNoTracking()
            .Where(u => u.Id == userId)
            .Select(u => new
            {
                u.FirstName,
                u.LastName,
                u.Email,
                u.Bio
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (user is null)
        {
            throw new InvalidOperationException("User profile not found.");
        }

        var sections = new Dictionary<string, object>();

        if (request.IncludeEducation)
        {
            var education = await _db.Educations
                .AsNoTracking()
                .Where(x => x.UserId == userId)
                .OrderByDescending(x => x.Id)
                .Select(x => new
                {
                    x.Institution,
                    x.Degree,
                    x.FieldOfStudy,
                    x.StartDate,
                    x.EndDate,
                    x.GPA,
                    x.Description
                })
                .ToListAsync(cancellationToken);

            sections["education"] = education;
        }

        if (request.IncludeExperience)
        {
            var experience = await _db.Experiences
                .AsNoTracking()
                .Where(x => x.UserId == userId)
                .OrderByDescending(x => x.Id)
                .Select(x => new
                {
                    x.Company,
                    x.JobTitle,
                    x.Location,
                    x.StartDate,
                    x.EndDate,
                    x.IsCurrentJob,
                    x.Responsibilities
                })
                .ToListAsync(cancellationToken);

            sections["experience"] = experience;
        }

        if (request.IncludeSkills)
        {
            var skills = await _db.Skills
                .AsNoTracking()
                .Where(x => x.UserId == userId)
                .OrderByDescending(x => x.Id)
                .Select(x => new
                {
                    x.Name,
                    x.Category,
                    x.ProficiencyLevel
                })
                .ToListAsync(cancellationToken);

            sections["skills"] = skills;
        }

        if (request.IncludeProjects)
        {
            var projects = await _db.Projects
                .AsNoTracking()
                .Where(x => x.UserId == userId)
                .OrderByDescending(x => x.Id)
                .Select(x => new
                {
                    x.Title,
                    x.Description,
                    x.TechnologiesUsed,
                    x.ProjectUrl,
                    x.StartDate,
                    x.EndDate
                })
                .ToListAsync(cancellationToken);

            sections["projects"] = projects;
        }

        if (request.IncludeCertifications)
        {
            var certifications = await _db.Certifications
                .AsNoTracking()
                .Where(x => x.UserId == userId)
                .OrderByDescending(x => x.Id)
                .Select(x => new
                {
                    x.Name,
                    x.IssuingOrganization,
                    x.IssueDate,
                    x.ExpirationDate,
                    x.CredentialId,
                    x.CredentialUrl
                })
                .ToListAsync(cancellationToken);

            sections["certifications"] = certifications;
        }

        var generationInput = new
        {
            request = new
            {
                request.JobTitle,
                request.TargetCompany,
                request.JobDescription,
                request.ExperienceLevel,
                request.ResumeFormat,
                request.PersonalSummary,
                request.IncludeEducation,
                request.IncludeExperience,
                request.IncludeSkills,
                request.IncludeProjects,
                request.IncludeCertifications
            },
            user = new
            {
                user.FirstName,
                user.LastName,
                user.Email,
                user.Bio
            },
            sections
        };

        var serializerOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web)
        {
            WriteIndented = false
        };

        var generationRequestJson = JsonSerializer.Serialize(generationInput, serializerOptions);

        var prompt = $"""
You are generating a resume draft in JSON format.
Use ONLY the supplied profile facts and selected sections.
Return JSON only, without markdown or explanation.

Mandatory rules:
- Do not fabricate companies, schools, dates, URLs, or credentials.
- Tailor, reorder, summarize, and extract bullets from supplied facts.
- If a selected section has no data, return an empty array.
- Do not invent new field types. Use only these top-level fields: user, education, experience, skills, projects, certifications.
- If personalSummary is provided, treat it as extra summary context only, not a canonical profile field.
- Do not include profileImageUrl in any part of the output.
- For experience items, you may generate concise bullet points only by reformulating and combining keywords/tasks explicitly present in the provided job description, responsibilities, projects, skills, certifications, and personal summary.
- Do not create brand-new claims; every bullet must be traceable to supplied profile facts.

Input JSON:
{generationRequestJson}
""";

        return new ResumeGenerationPayload(prompt, generationRequestJson);
    }
}
