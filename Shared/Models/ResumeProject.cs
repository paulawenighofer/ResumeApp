using System.ComponentModel.DataAnnotations;

namespace Shared.Models;

public class ResumeProject
{
    public int Id { get; set; }

    [Required]
    public string UserId { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Url { get; set; }
    public string? Technologies { get; set; }
    public DateOnly? StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
}
