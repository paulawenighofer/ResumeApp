using API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Models;
using System.Security.Claims;

namespace API.Controllers;

[ApiController]
[Authorize]
[Route("api/educations")]
public class EducationsController : ControllerBase
{
    private readonly InMemoryResumeStore _store;
    private readonly ApiMetrics _metrics;

    public EducationsController(InMemoryResumeStore store, ApiMetrics metrics)
    {
        _store = store;
        _metrics = metrics;
    }

    [HttpGet]
    public ActionResult<IEnumerable<Education>> GetAll()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized();
        }

        return Ok(_store.Educations.Where(x => x.UserId == userId));
    }

    [HttpGet("{id:int}")]
    public ActionResult<Education> GetById(int id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized();
        }

        var education = _store.Educations.FirstOrDefault(x => x.Id == id && x.UserId == userId);
        return education is null ? NotFound() : Ok(education);
    }

    [HttpPost]
    public ActionResult<Education> Create([FromBody] Education education)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized();
        }

        education.Id = _store.NextEducationId();
        education.UserId = userId;
        _store.Educations.Add(education);
        _metrics.EducationsCreated.Add(1);
        return CreatedAtAction(nameof(GetById), new { id = education.Id }, education);
    }

    [HttpPut("{id:int}")]
    public ActionResult<Education> Update(int id, [FromBody] Education education)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized();
        }

        var index = _store.Educations.FindIndex(x => x.Id == id && x.UserId == userId);
        if (index < 0)
        {
            return NotFound();
        }

        education.Id = id;
        education.UserId = userId;
        _store.Educations[index] = education;
        return Ok(education);
    }

    [HttpDelete("{id:int}")]
    public IActionResult Delete(int id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized();
        }

        var education = _store.Educations.FirstOrDefault(x => x.Id == id && x.UserId == userId);
        if (education is null)
        {
            return NotFound();
        }

        _store.Educations.Remove(education);
        return NoContent();
    }
}
