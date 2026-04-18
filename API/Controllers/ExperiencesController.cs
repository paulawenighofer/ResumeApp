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
[Route("api/experiences")]
public class ExperiencesController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ApiMetrics _metrics;

    public ExperiencesController(AppDbContext db, ApiMetrics metrics)
    {
        _db = db;
        _metrics = metrics;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Experience>>> GetAll()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized();
        }

        var items = await _db.Experiences
            .Where(x => x.UserId == userId)
            .OrderByDescending(x => x.Id)
            .ToListAsync();

        return Ok(items);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<Experience>> GetById(int id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized();
        }

        var experience = await _db.Experiences
            .FirstOrDefaultAsync(x => x.Id == id && x.UserId == userId);

        return experience is null ? NotFound() : Ok(experience);
    }

    [HttpPost]
    public async Task<ActionResult<Experience>> Create([FromBody] Experience experience)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized();
        }

        experience.Id = 0;
        experience.UserId = userId;
        experience.StartDate = ToUtcDateTime(experience.StartDate);
        experience.EndDate = ToUtcDateTime(experience.EndDate);

        _db.Experiences.Add(experience);
        await _db.SaveChangesAsync();

        _metrics.RecordProfileMutation(TelemetryTags.Sections.Experience, TelemetryTags.Actions.Create, userId);
        return CreatedAtAction(nameof(GetById), new { id = experience.Id }, experience);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<Experience>> Update(int id, [FromBody] Experience experience)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized();
        }

        var existing = await _db.Experiences
            .FirstOrDefaultAsync(x => x.Id == id && x.UserId == userId);

        if (existing is null)
        {
            return NotFound();
        }

        existing.Company = experience.Company;
        existing.JobTitle = experience.JobTitle;
        existing.Location = experience.Location;
        existing.StartDate = ToUtcDateTime(experience.StartDate);
        existing.EndDate = ToUtcDateTime(experience.EndDate);
        existing.IsCurrentJob = experience.IsCurrentJob;
        existing.Responsibilities = experience.Responsibilities;

        await _db.SaveChangesAsync();
        _metrics.RecordProfileMutation(TelemetryTags.Sections.Experience, TelemetryTags.Actions.Update, userId);
        return Ok(existing);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized();
        }

        var experience = await _db.Experiences
            .FirstOrDefaultAsync(x => x.Id == id && x.UserId == userId);

        if (experience is null)
        {
            return NotFound();
        }

        _db.Experiences.Remove(experience);
        await _db.SaveChangesAsync();
        _metrics.RecordProfileMutation(TelemetryTags.Sections.Experience, TelemetryTags.Actions.Delete, userId);
        return NoContent();
    }

    private static DateTime ToUtcDateTime(DateTime value)
        => DateTime.SpecifyKind(value, DateTimeKind.Utc);

    private static DateTime? ToUtcDateTime(DateTime? value)
        => value.HasValue
            ? DateTime.SpecifyKind(value.Value, DateTimeKind.Utc)
            : null;
}
