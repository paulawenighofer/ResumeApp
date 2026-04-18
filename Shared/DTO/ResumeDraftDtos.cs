using System.ComponentModel.DataAnnotations;
using Shared.Models;

namespace Shared.DTO;

public class CreateResumeDraftRequest
{
    [Required, MaxLength(200)]
    public string JobTitle { get; set; } = string.Empty;

    [Required, MaxLength(200)]
    public string TargetCompany { get; set; } = string.Empty;

    [MaxLength(8000)]
    public string? JobDescription { get; set; }

    [MaxLength(100)]
    public string? ExperienceLevel { get; set; }

    [MaxLength(100)]
    public string? ResumeFormat { get; set; }

    [MaxLength(4000)]
    public string? PersonalSummary { get; set; }

    public bool IncludeEducation { get; set; } = true;
    public bool IncludeExperience { get; set; } = true;
    public bool IncludeSkills { get; set; } = true;
    public bool IncludeProjects { get; set; } = true;
    public bool IncludeCertifications { get; set; } = true;
}

public class ResumeDraftResponse
{
    public int Id { get; set; }
    public ResumeDraftStatus Status { get; set; }
    public string TargetCompany { get; set; } = string.Empty;
    public string? FailedReason { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class ResumeListItemDto
{
    public int Id { get; set; }
    public ResumeDraftStatus Status { get; set; }
    public string TargetCompany { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class ResumeDetailDto
{
    public int Id { get; set; }
    public ResumeDraftStatus Status { get; set; }
    public string TargetCompany { get; set; } = string.Empty;
    public string? GenerationRequestJson { get; set; }
    public string? GeneratedResumeJson { get; set; }
    public string? EditedResumeJson { get; set; }
    public string? ApprovedJson { get; set; }
    public bool HasPdf { get; set; }
    public DateTime? PdfGeneratedAt { get; set; }
    public string? PdfFailureReason { get; set; }
    public string? FailedReason { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public DateTime? ApprovedAt { get; set; }
}

public class SaveDraftEditRequest
{
    [Required, MaxLength(20000)]
    public string EditedResumeJson { get; set; } = string.Empty;
}

public class ApproveDraftRequest
{
    [Required, MaxLength(20000)]
    public string FinalResumeJson { get; set; } = string.Empty;
}

public class ApproveDraftResponse
{
    public int Id { get; set; }
    public ResumeDraftStatus Status { get; set; }
    public DateTime ApprovedAt { get; set; }
    public string TargetCompany { get; set; } = string.Empty;
}
