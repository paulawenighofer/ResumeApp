using API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Models;
using System.Security.Claims;

namespace API.Controllers;

[ApiController]
[Authorize]
[Route("api/projects")]
public class ProjectsController : ControllerBase
{
    private readonly InMemoryResumeStore _store;

    public ProjectsController(InMemoryResumeStore store)
    {
        _store = store;
    }

    [HttpGet]
    public ActionResult<IEnumerable<ResumeProject>> GetAll()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized();
        }

        return Ok(_store.Projects.Where(x => x.UserId == userId));
    }

    [HttpGet("{id:int}")]
    public ActionResult<ResumeProject> GetById(int id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized();
        }

        var project = _store.Projects.FirstOrDefault(x => x.Id == id && x.UserId == userId);
        return project is null ? NotFound() : Ok(project);
    }

    [HttpPost]
    public ActionResult<ResumeProject> Create([FromBody] ResumeProject project)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized();
        }

        project.Id = _store.NextProjectId();
        project.UserId = userId;
        _store.Projects.Add(project);
        return CreatedAtAction(nameof(GetById), new { id = project.Id }, project);
    }

    [HttpPut("{id:int}")]
    public ActionResult<ResumeProject> Update(int id, [FromBody] ResumeProject project)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized();
        }

        var index = _store.Projects.FindIndex(x => x.Id == id && x.UserId == userId);
        if (index < 0)
        {
            return NotFound();
        }

        project.Id = id;
        project.UserId = userId;
        _store.Projects[index] = project;
        return Ok(project);
    }

    [HttpDelete("{id:int}")]
    public IActionResult Delete(int id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized();
        }

        var project = _store.Projects.FirstOrDefault(x => x.Id == id && x.UserId == userId);
        if (project is null)
        {
            return NotFound();
        }

        _store.Projects.Remove(project);
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

        var project = _store.Projects.FirstOrDefault(x => x.Id == id && x.UserId == userId);
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
}
