using System.Text.Json.Serialization;

namespace ResumeApp.Models;

public class StoredResumeEntry : ILocalSyncEntity
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public DateTimeOffset? UpdatedAt { get; set; }
    public string? Version { get; set; }
    public bool Deleted { get; set; }
    public string TargetJobTitle { get; set; } = string.Empty;
    public string? TargetCompany { get; set; }
    public string? JobDescription { get; set; }
    public string? CompanyDescription { get; set; }
    public string? GeneratedContent { get; set; }
    public string? PdfBlobUrl { get; set; }
    [JsonIgnore]
    public string? LocalFilePath { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ResumeUpdatedAt { get; set; }
}
