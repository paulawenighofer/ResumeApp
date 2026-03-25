namespace Shared.Models;

public class Experience
{
    public int Id { get; set; }
    public string Company { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? EmploymentType { get; set; }
    public string? Location { get; set; }
    public DateOnly? StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
    public string? Description { get; set; }
}
