using CommunityToolkit.Datasync.Server.EntityFrameworkCore;

namespace API.Models.Sync;

public class SyncEducation : RepositoryControlledEntityTableData, IUserOwnedSyncEntity
{
    public string UserId { get; set; } = string.Empty;
    public string School { get; set; } = string.Empty;
    public string Degree { get; set; } = string.Empty;
    public string FieldOfStudy { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string? GPA { get; set; }
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
