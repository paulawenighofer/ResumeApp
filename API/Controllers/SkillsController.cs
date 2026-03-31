using API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Models;
using System.Security.Claims;

namespace API.Controllers;

[ApiController]
[Authorize]
[Route("api/skills")]
public class SkillsController : ControllerBase
{
    private readonly InMemoryResumeStore _store;
    private readonly ApiMetrics _metrics;

    public SkillsController(InMemoryResumeStore store, ApiMetrics metrics)
    {
        _store = store;
        _metrics = metrics;
    }

    [HttpGet]
    public ActionResult<IEnumerable<Skill>> GetAll()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized();
        }

        return Ok(_store.Skills.Where(x => x.UserId == userId));
    }

    [HttpGet("{id:int}")]
    public ActionResult<Skill> GetById(int id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized();
        }

        var skill = _store.Skills.FirstOrDefault(x => x.Id == id && x.UserId == userId);
        return skill is null ? NotFound() : Ok(skill);
    }

    [HttpPost]
    public ActionResult<Skill> Create([FromBody] Skill skill)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized();
        }

        skill.Id = _store.NextSkillId();
        skill.UserId = userId;
        _store.Skills.Add(skill);
        _metrics.SkillsCreated.Add(1);
        return CreatedAtAction(nameof(GetById), new { id = skill.Id }, skill);
    }

    [HttpPut("{id:int}")]
    public ActionResult<Skill> Update(int id, [FromBody] Skill skill)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized();
        }

        var index = _store.Skills.FindIndex(x => x.Id == id && x.UserId == userId);
        if (index < 0)
        {
            return NotFound();
        }

        skill.Id = id;
        skill.UserId = userId;
        _store.Skills[index] = skill;
        return Ok(skill);
    }

    [HttpDelete("{id:int}")]
    public IActionResult Delete(int id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized();
        }

        var skill = _store.Skills.FirstOrDefault(x => x.Id == id && x.UserId == userId);
        if (skill is null)
        {
            return NotFound();
        }

        _store.Skills.Remove(skill);
        return NoContent();
    }
}
