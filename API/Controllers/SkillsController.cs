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
[Route("api/skills")]
public class SkillsController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ApiMetrics _metrics;

    public SkillsController(AppDbContext db, ApiMetrics metrics)
    {
        _db = db;
        _metrics = metrics;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Skill>>> GetAll()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized();
        }

        var items = await _db.Skills
            .Where(x => x.UserId == userId)
            .OrderByDescending(x => x.Id)
            .ToListAsync();

        return Ok(items);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<Skill>> GetById(int id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized();
        }

        var skill = await _db.Skills
            .FirstOrDefaultAsync(x => x.Id == id && x.UserId == userId);

        return skill is null ? NotFound() : Ok(skill);
    }

    [HttpPost]
    public async Task<ActionResult<Skill>> Create([FromBody] Skill skill)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized();
        }

        skill.Id = 0;
        skill.UserId = userId;

        _db.Skills.Add(skill);
        await _db.SaveChangesAsync();

        _metrics.RecordProfileMutation(TelemetryTags.Sections.Skill, TelemetryTags.Actions.Create, userId);
        return CreatedAtAction(nameof(GetById), new { id = skill.Id }, skill);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<Skill>> Update(int id, [FromBody] Skill skill)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized();
        }

        var existing = await _db.Skills
            .FirstOrDefaultAsync(x => x.Id == id && x.UserId == userId);

        if (existing is null)
        {
            return NotFound();
        }

        existing.Name = skill.Name;
        existing.Category = skill.Category;
        existing.ProficiencyLevel = skill.ProficiencyLevel;

        await _db.SaveChangesAsync();
        _metrics.RecordProfileMutation(TelemetryTags.Sections.Skill, TelemetryTags.Actions.Update, userId);
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

        var skill = await _db.Skills
            .FirstOrDefaultAsync(x => x.Id == id && x.UserId == userId);

        if (skill is null)
        {
            return NotFound();
        }

        _db.Skills.Remove(skill);
        await _db.SaveChangesAsync();
        _metrics.RecordProfileMutation(TelemetryTags.Sections.Skill, TelemetryTags.Actions.Delete, userId);
        return NoContent();
    }
}
