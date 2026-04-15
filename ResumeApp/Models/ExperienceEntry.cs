namespace ResumeApp.Models;

public class ExperienceEntry : ILocalSyncEntity
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public DateTimeOffset? UpdatedAt { get; set; }
    public string? Version { get; set; }
    public bool Deleted { get; set; }
    public string Company { get; set; } = "";
    public string JobTitle { get; set; } = "";
    public string EmploymentType { get; set; } = "Full-time";
    public string Location { get; set; } = "";
    public DateTime StartDate { get; set; } = DateTime.Now;
    public DateTime EndDate { get; set; } = DateTime.Now.AddYears(1);
    public bool IsCurrentJob { get; set; }
    public string Description { get; set; } = "";
    public string? Technologies { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public string DurationText
    {
        get
        {
            var endDate = IsCurrentJob ? DateTime.Now : EndDate;
            var duration = endDate - StartDate;
            int years = (int)(duration.TotalDays / 365.25);
            int months = (int)((duration.TotalDays % 365.25) / 30.44);

            if (years > 0 && months > 0)
                return $"{years}y {months}m";
            else if (years > 0)
                return $"{years} year{(years > 1 ? "s" : "")}";
            else
                return $"{months} month{(months > 1 ? "s" : "")}";
        }
    }

    public List<string> TechList => Technologies?.Split(',')
        .Select(t => t.Trim())
        .Where(t => !string.IsNullOrEmpty(t))
        .ToList() ?? new();
}
