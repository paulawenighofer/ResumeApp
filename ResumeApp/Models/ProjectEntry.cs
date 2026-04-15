using System.Text.Json.Serialization;

namespace ResumeApp.Models;

public class ProjectEntry : ILocalSyncEntity
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public DateTimeOffset? UpdatedAt { get; set; }
    public string? Version { get; set; }
    public bool Deleted { get; set; }
    public string Name { get; set; } = "";
    public string ProjectType { get; set; } = "Personal Project";
    public string Description { get; set; } = "";
    public string? Technologies { get; set; }
    public string? ProjectUrl { get; set; }
    public DateTime StartDate { get; set; } = DateTime.Now;
    public DateTime EndDate { get; set; } = DateTime.Now.AddMonths(3);
    public string ImagePathsJson { get; set; } = "[]";

    [JsonIgnore]
    public List<string> ImagePaths
    {
        get => string.IsNullOrWhiteSpace(ImagePathsJson)
            ? []
            : System.Text.Json.JsonSerializer.Deserialize<List<string>>(ImagePathsJson) ?? [];
        set => ImagePathsJson = System.Text.Json.JsonSerializer.Serialize(value ?? []);
    }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public string DurationText
    {
        get
        {
            var duration = EndDate - StartDate;
            int months = (int)(duration.TotalDays / 30.44);
            return months > 0 ? $"{months} month{(months > 1 ? "s" : "")}" : "< 1 month";
        }
    }

    public List<string> TechList => Technologies?.Split(',')
        .Select(t => t.Trim())
        .Where(t => !string.IsNullOrEmpty(t))
        .ToList() ?? new();

    public bool HasImages => ImagePaths.Count > 0;
}
