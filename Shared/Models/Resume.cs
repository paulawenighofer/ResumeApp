using System.ComponentModel.DataAnnotations;

namespace Shared.Models;

public class Resume
{
    public int Id { get; set; }

    public string UserId { get; set; } = string.Empty;

    public ResumeDraftStatus Status { get; set; } = ResumeDraftStatus.Pending;

    [Required, MaxLength(200)]
    public string TargetCompany { get; set; } = string.Empty;

    [MaxLength(20000)]
    public string? GenerationRequestJson { get; set; }

    [MaxLength(20000)]
    public string? GeneratedResumeJson { get; set; }

    [MaxLength(20000)]
    public string? EditedResumeJson { get; set; }

    [MaxLength(2000)]
    public string? FailedReason { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }
}
