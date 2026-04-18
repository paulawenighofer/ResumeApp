using API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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
}
