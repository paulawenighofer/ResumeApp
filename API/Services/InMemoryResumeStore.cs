using Shared.Models;

namespace API.Services;

public class InMemoryResumeStore
{
    public List<Education> Educations { get; } = [];
    public List<Experience> Experiences { get; } = [];
    public List<Skill> Skills { get; } = [];
    public List<ResumeProject> Projects { get; } = [];

    private int _educationId = 1;
    private int _experienceId = 1;
    private int _skillId = 1;
    private int _projectId = 1;

    public int NextEducationId() => _educationId++;
    public int NextExperienceId() => _experienceId++;
    public int NextSkillId() => _skillId++;
    public int NextProjectId() => _projectId++;
}
