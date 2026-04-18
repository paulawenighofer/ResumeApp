using Shared.Models;

namespace ResumeApp.Models;

public class ResumeDraftListItem
{
    public int Id { get; set; }
    public string TargetCompany { get; set; } = string.Empty;
    public ResumeDraftStatus Status { get; set; }
    public string StatusText { get; set; } = string.Empty;
    public string StatusColorHex { get; set; } = "#6B7280";
    public string? FailedReason { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string CreatedAtText { get; set; } = string.Empty;
}
