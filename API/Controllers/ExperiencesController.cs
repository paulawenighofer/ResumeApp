using API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Models;
using System.Security.Claims;

namespace API.Controllers;

[ApiController]
[Authorize]
[Route("api/experiences")]
public class ExperiencesController : ControllerBase
{
    private readonly InMemoryResumeStore _store;

    public ExperiencesController(InMemoryResumeStore store)
    {
        _store = store;
    }

    [HttpGet]
    public ActionResult<IEnumerable<Experience>> GetAll()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized();
        }

        return Ok(_store.Experiences.Where(x => x.UserId == userId));
    }

    [HttpGet("{id:int}")]
    public ActionResult<Experience> GetById(int id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized();
        }

        var experience = _store.Experiences.FirstOrDefault(x => x.Id == id && x.UserId == userId);
        return experience is null ? NotFound() : Ok(experience);
    }

    [HttpPost]
    public ActionResult<Experience> Create([FromBody] Experience experience)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized();
        }

        experience.Id = _store.NextExperienceId();
        experience.UserId = userId;
        _store.Experiences.Add(experience);
        return CreatedAtAction(nameof(GetById), new { id = experience.Id }, experience);
    }

    [HttpPut("{id:int}")]
    public ActionResult<Experience> Update(int id, [FromBody] Experience experience)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized();
        }

        var index = _store.Experiences.FindIndex(x => x.Id == id && x.UserId == userId);
        if (index < 0)
        {
            return NotFound();
        }

        experience.Id = id;
        experience.UserId = userId;
        _store.Experiences[index] = experience;
        return Ok(experience);
    }

    [HttpDelete("{id:int}")]
    public IActionResult Delete(int id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized();
        }

        var experience = _store.Experiences.FirstOrDefault(x => x.Id == id && x.UserId == userId);
        if (experience is null)
        {
            return NotFound();
        }

        _store.Experiences.Remove(experience);
        return NoContent();
    }
}
