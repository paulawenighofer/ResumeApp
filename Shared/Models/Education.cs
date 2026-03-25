namespace Shared.Models;

public class Education
{
    public int Id { get; set; }
    public string School { get; set; } = string.Empty;
    public string Degree { get; set; } = string.Empty;
    public string FieldOfStudy { get; set; } = string.Empty;
    public DateOnly? StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
    public string? Description { get; set; }
}
