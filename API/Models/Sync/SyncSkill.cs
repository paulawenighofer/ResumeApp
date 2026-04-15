using CommunityToolkit.Datasync.Server.EntityFrameworkCore;

namespace API.Models.Sync;

public class SyncSkill : RepositoryControlledEntityTableData, IUserOwnedSyncEntity
{
    public string UserId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string ProficiencyLevel { get; set; } = "Intermediate";
    public string Category { get; set; } = "Programming Language";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
