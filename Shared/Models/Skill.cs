namespace Shared.Models;

public class Skill
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Category { get; set; }
    public int? Proficiency { get; set; }
}
