using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Shared.Models;
using System.Security.Claims;
using API.Services;
using System.Diagnostics;

namespace API.Controllers;

[ApiController]
[Authorize]
[Route("api/profile")]
public class ProfileController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ApiMetrics _metrics;
    private readonly IFileStorageService _fileStorage;

    public ProfileController(UserManager<ApplicationUser> userManager, ApiMetrics metrics, IFileStorageService fileStorage)
    {
        _userManager = userManager;
        _metrics = metrics;
        _fileStorage = fileStorage;
    }

    [HttpPost("image")]
    [RequestSizeLimit(10_000_000)]
    public async Task<IActionResult> UploadProfileImage(IFormFile? file)
    {
        if (file is null || file.Length == 0)
        {
            _metrics.RecordUpload(TelemetryTags.Sections.ProfileImage, TelemetryTags.Outcomes.Failure, 0, 0, 0);
            return BadRequest(new { message = "No image uploaded." });
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized();
        }

        var stopwatch = Stopwatch.StartNew();
        var extension = Path.GetExtension(file.FileName);
        var fileName = $"{userId}_{Guid.NewGuid():N}{extension}";
        await using var uploadStream = file.OpenReadStream();

        var user = await _userManager.FindByIdAsync(userId);
        if (user is null)
        {
            return Unauthorized();
        }

        user.ProfileImageUrl = await _fileStorage.SaveAsync(uploadStream, fileName, "profiles", Request);
        await _userManager.UpdateAsync(user);
        stopwatch.Stop();
        _metrics.RecordUpload(TelemetryTags.Sections.ProfileImage, TelemetryTags.Outcomes.Success, 1, file.Length, stopwatch.Elapsed.TotalMilliseconds, userId);

        return Ok(new { imageUrl = user.ProfileImageUrl });
    }
}
