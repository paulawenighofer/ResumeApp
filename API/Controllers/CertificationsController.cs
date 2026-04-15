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
[Route("api/certifications")]
public class CertificationsController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ApiMetrics _metrics;

    public CertificationsController(AppDbContext db, ApiMetrics metrics)
    {
        _db = db;
        _metrics = metrics;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Certification>>> GetAll()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized();
        }

        return Ok(await _db.Certifications.Where(x => x.UserId == userId).OrderByDescending(x => x.Id).ToListAsync());
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<Certification>> GetById(int id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized();
        }

        var certification = await _db.Certifications.FirstOrDefaultAsync(x => x.Id == id && x.UserId == userId);
        return certification is null ? NotFound() : Ok(certification);
    }

    [HttpPost]
    public async Task<ActionResult<Certification>> Create([FromBody] Certification certification)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized();
        }

        certification.Id = 0;
        certification.UserId = userId;
        _db.Certifications.Add(certification);
        await _db.SaveChangesAsync();
        _metrics.RecordProfileMutation("certification", TelemetryTags.Actions.Create, userId);
        return CreatedAtAction(nameof(GetById), new { id = certification.Id }, certification);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<Certification>> Update(int id, [FromBody] Certification certification)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized();
        }

        var existing = await _db.Certifications.FirstOrDefaultAsync(x => x.Id == id && x.UserId == userId);
        if (existing is null)
        {
            return NotFound();
        }

        existing.Name = certification.Name;
        existing.IssuingOrganization = certification.IssuingOrganization;
        existing.IssueDate = certification.IssueDate;
        existing.ExpirationDate = certification.ExpirationDate;
        existing.CredentialId = certification.CredentialId;
        existing.CredentialUrl = certification.CredentialUrl;

        await _db.SaveChangesAsync();
        _metrics.RecordProfileMutation("certification", TelemetryTags.Actions.Update, userId);
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

        var certification = await _db.Certifications.FirstOrDefaultAsync(x => x.Id == id && x.UserId == userId);
        if (certification is null)
        {
            return NotFound();
        }

        _db.Certifications.Remove(certification);
        await _db.SaveChangesAsync();
        _metrics.RecordProfileMutation("certification", TelemetryTags.Actions.Delete, userId);
        return NoContent();
    }
}
