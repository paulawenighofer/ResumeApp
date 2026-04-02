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
[Route("api/educations")]
public class EducationsController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ApiMetrics _metrics;

    public EducationsController(AppDbContext db, ApiMetrics metrics)
    {
        _db = db;
        _metrics = metrics;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Education>>> GetAll()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized();
        }

        var items = await _db.Educations
            .Where(x => x.UserId == userId)
            .OrderByDescending(x => x.Id)
            .ToListAsync();

        return Ok(items);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<Education>> GetById(int id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized();
        }

        var education = await _db.Educations
            .FirstOrDefaultAsync(x => x.Id == id && x.UserId == userId);

        return education is null ? NotFound() : Ok(education);
    }

    [HttpPost]
    public async Task<ActionResult<Education>> Create([FromBody] Education education)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized();
        }

        education.Id = 0;
        education.UserId = userId;

        _db.Educations.Add(education);
        await _db.SaveChangesAsync();

        _metrics.EducationsCreated.Add(1);
        return CreatedAtAction(nameof(GetById), new { id = education.Id }, education);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<Education>> Update(int id, [FromBody] Education education)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized();
        }

        var existing = await _db.Educations
            .FirstOrDefaultAsync(x => x.Id == id && x.UserId == userId);

        if (existing is null)
        {
            return NotFound();
        }

        existing.Institution = education.Institution;
        existing.Degree = education.Degree;
        existing.FieldOfStudy = education.FieldOfStudy;
        existing.StartDate = education.StartDate;
        existing.EndDate = education.EndDate;
        existing.GPA = education.GPA;
        existing.Description = education.Description;

        await _db.SaveChangesAsync();
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

        var education = await _db.Educations
            .FirstOrDefaultAsync(x => x.Id == id && x.UserId == userId);

        if (education is null)
        {
            return NotFound();
        }

        _db.Educations.Remove(education);
        await _db.SaveChangesAsync();
        return NoContent();
    }
}
