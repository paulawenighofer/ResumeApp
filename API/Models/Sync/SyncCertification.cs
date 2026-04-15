using CommunityToolkit.Datasync.Server.EntityFrameworkCore;

namespace API.Models.Sync;

public class SyncCertification : RepositoryControlledEntityTableData, IUserOwnedSyncEntity
{
    public string UserId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string IssuingOrganization { get; set; } = string.Empty;
    public DateTime? IssueDate { get; set; }
    public DateTime? ExpirationDate { get; set; }
    public string? CredentialId { get; set; }
    public string? CredentialUrl { get; set; }
}
