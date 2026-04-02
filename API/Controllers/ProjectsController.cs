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
            ProjectUrl = project.Url,
            StartDate = project.StartDate?.ToDateTime(TimeOnly.MinValue),
            EndDate = project.EndDate?.ToDateTime(TimeOnly.MinValue)
        };

        _db.Projects.Add(entity);
        await _db.SaveChangesAsync();

        _metrics.ProjectsCreated.Add(1);

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
        entity.ProjectUrl = project.Url;
        entity.StartDate = project.StartDate?.ToDateTime(TimeOnly.MinValue);
        entity.EndDate = project.EndDate?.ToDateTime(TimeOnly.MinValue);

        await _db.SaveChangesAsync();

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
        return NoContent();
    }

    [HttpPost("{id:int}/images")]
    [RequestSizeLimit(25_000_000)]
    public async Task<IActionResult> UploadImages(int id, [FromForm] List<IFormFile> files, [FromServices] IWebHostEnvironment environment)
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

        if (files.Count == 0)
        {
            return BadRequest(new { message = "No images uploaded." });
        }

        var uploadsFolder = Path.Combine(environment.WebRootPath, "uploads", "projects", id.ToString());
        Directory.CreateDirectory(uploadsFolder);

        var uploadedUrls = new List<string>();

        foreach (var file in files.Where(f => f.Length > 0))
        {
            var extension = Path.GetExtension(file.FileName);
            var fileName = $"{Guid.NewGuid():N}{extension}";
            var fullPath = Path.Combine(uploadsFolder, fileName);

            await using var stream = System.IO.File.Create(fullPath);
            await file.CopyToAsync(stream);

            uploadedUrls.Add($"{Request.Scheme}://{Request.Host}/uploads/projects/{id}/{fileName}");
        }

        return Ok(new { images = uploadedUrls });
    }

    private static ResumeProject MapToResumeProject(Project project) => new()
    {
        Id = project.Id,
        UserId = project.UserId,
        Name = project.Title,
        Description = project.Description,
        Url = project.ProjectUrl,
        StartDate = project.StartDate.HasValue
            ? DateOnly.FromDateTime(project.StartDate.Value)
            : null,
        EndDate = project.EndDate.HasValue
            ? DateOnly.FromDateTime(project.EndDate.Value)
            : null
    };
}
