using API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Shared.DTO;
using System.Security.Claims;

namespace API.Controllers;

[ApiController]
[Authorize]
[Route("api/resumes")]
public class ResumesController : ControllerBase
{
    private readonly IResumeDraftService _resumeDraftService;

    public ResumesController(IResumeDraftService resumeDraftService)
    {
        _resumeDraftService = resumeDraftService;
    }

    [HttpPost("drafts")]
    [EnableRateLimiting("resume-generation")]
    public async Task<ActionResult<ResumeDraftResponse>> CreateDraft([FromBody] CreateResumeDraftRequest request, CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized();
        }

        var draft = await _resumeDraftService.CreateDraftAsync(userId, request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = draft.Id }, draft);
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ResumeListItemDto>>> GetAll(CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized();
        }

        var items = await _resumeDraftService.ListDraftsAsync(userId, cancellationToken);
        return Ok(items);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ResumeDetailDto>> GetById(int id, CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized();
        }

        var item = await _resumeDraftService.GetDraftAsync(userId, id, cancellationToken);
        return item is null ? NotFound() : Ok(item);
    }

    [HttpPut("{id:int}/draft")]
    public async Task<ActionResult<ResumeDetailDto>> SaveDraftEdit(int id, [FromBody] SaveDraftEditRequest request, CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized();
        }

        try
        {
            var updated = await _resumeDraftService.SaveDraftEditAsync(userId, id, request, cancellationToken);
            return updated is null ? NotFound() : Ok(updated);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("{id:int}/approve")]
    public async Task<ActionResult<ApproveDraftResponse>> ApproveDraft(int id, [FromBody] ApproveDraftRequest request, CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized();
        }

        try
        {
            var approved = await _resumeDraftService.ApproveDraftAsync(userId, id, request, cancellationToken);
            return approved is null ? NotFound() : Ok(approved);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
