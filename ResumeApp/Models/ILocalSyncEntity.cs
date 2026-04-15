namespace ResumeApp.Models;

public interface ILocalSyncEntity
{
    string Id { get; set; }
    DateTimeOffset? UpdatedAt { get; set; }
    string? Version { get; set; }
    bool Deleted { get; set; }
}
