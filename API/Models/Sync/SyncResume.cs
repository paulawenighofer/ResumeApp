using CommunityToolkit.Datasync.Server.EntityFrameworkCore;

namespace API.Models.Sync;

public class SyncResume : RepositoryControlledEntityTableData, IUserOwnedSyncEntity
{
    public string UserId { get; set; } = string.Empty;
    public string TargetJobTitle { get; set; } = string.Empty;
    public string? TargetCompany { get; set; }
    public string? JobDescription { get; set; }
    public string? CompanyDescription { get; set; }
    public string? GeneratedContent { get; set; }
    public string? PdfBlobUrl { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ResumeUpdatedAt { get; set; }
}
