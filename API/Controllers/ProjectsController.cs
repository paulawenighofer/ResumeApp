using API.Data;
using API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Shared.Models;
using System.Security.Claims;

namespace API.Controllers;

[ApiController]
[Authorize]
[Route("api/projects")]
public class ProjectsController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ApiMetrics _metrics;

    public ProjectsController(AppDbContext db, ApiMetrics metrics)
    {
        _db = db;
        _metrics = metrics;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<ResumeProject>>> GetAll()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized();
        }

        var entities = await _db.Projects
            .Where(x => x.UserId == userId)
            .OrderByDescending(x => x.Id)
            .ToListAsync();

        var items = entities.Select(MapToResumeProject);
        return Ok(items);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ResumeProject>> GetById(int id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized();
        }

        var entity = await _db.Projects
            .FirstOrDefaultAsync(x => x.Id == id && x.UserId == userId);

        return entity is null ? NotFound() : Ok(MapToResumeProject(entity));
    }

    [HttpPost]
    public async Task<ActionResult<ResumeProject>> Create([FromBody] ResumeProject project)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized();
        }

        var entity = new Project
        {
            UserId = userId,
            Title = project.Name,
            Description = project.Description,
            TechnologiesUsed = project.Technologies,
            ProjectUrl = project.Url,
            StartDate = ToUtcDateTime(project.StartDate),
            EndDate = ToUtcDateTime(project.EndDate)
        };

        _db.Projects.Add(entity);
        await _db.SaveChangesAsync();

        _metrics.RecordProfileMutation(TelemetryTags.Sections.Project, TelemetryTags.Actions.Create, userId);

        var response = new ResumeProject
        {
            Id = entity.Id,
            UserId = entity.UserId,
            Name = entity.Title,
            Description = entity.Description,
            Url = entity.ProjectUrl,
            StartDate = entity.StartDate is null ? null : DateOnly.FromDateTime(entity.StartDate.Value),
            EndDate = entity.EndDate is null ? null : DateOnly.FromDateTime(entity.EndDate.Value)
        };

        return CreatedAtAction(nameof(GetById), new { id = entity.Id }, response);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<ResumeProject>> Update(int id, [FromBody] ResumeProject project)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized();
        }

        var entity = await _db.Projects
            .FirstOrDefaultAsync(x => x.Id == id && x.UserId == userId);

        if (entity is null)
        {
            return NotFound();
        }

        entity.Title = project.Name;
        entity.Description = project.Description;
        entity.TechnologiesUsed = project.Technologies;
        entity.ProjectUrl = project.Url;
        entity.StartDate = ToUtcDateTime(project.StartDate);
        entity.EndDate = ToUtcDateTime(project.EndDate);

        await _db.SaveChangesAsync();
        _metrics.RecordProfileMutation(TelemetryTags.Sections.Project, TelemetryTags.Actions.Update, userId);

        return Ok(MapToResumeProject(entity));
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized();
        }

        var project = await _db.Projects
            .FirstOrDefaultAsync(x => x.Id == id && x.UserId == userId);

        if (project is null)
        {
            return NotFound();
        }

        _db.Projects.Remove(project);
        await _db.SaveChangesAsync();
        _metrics.RecordProfileMutation(TelemetryTags.Sections.Project, TelemetryTags.Actions.Delete, userId);
        return NoContent();
    }

    private static ResumeProject MapToResumeProject(Project project) => new()
    {
        Id = project.Id,
        UserId = project.UserId,
        Name = project.Title,
        Description = project.Description,
        Technologies = project.TechnologiesUsed,
        Url = project.ProjectUrl,
        StartDate = project.StartDate.HasValue
            ? DateOnly.FromDateTime(project.StartDate.Value)
            : null,
        EndDate = project.EndDate.HasValue
            ? DateOnly.FromDateTime(project.EndDate.Value)
            : null
    };

    private static DateTime? ToUtcDateTime(DateOnly? value)
        => value.HasValue
            ? DateTime.SpecifyKind(value.Value.ToDateTime(TimeOnly.MinValue), DateTimeKind.Utc)
            : null;
}
