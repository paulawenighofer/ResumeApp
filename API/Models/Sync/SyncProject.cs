using CommunityToolkit.Datasync.Server.EntityFrameworkCore;

namespace API.Models.Sync;

public class SyncProject : RepositoryControlledEntityTableData, IUserOwnedSyncEntity
{
    public string UserId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string ProjectType { get; set; } = "Personal Project";
    public string Description { get; set; } = string.Empty;
    public string? Technologies { get; set; }
    public string? ProjectUrl { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string ImagePathsJson { get; set; } = "[]";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
