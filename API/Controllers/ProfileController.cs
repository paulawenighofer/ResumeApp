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
    private readonly IWebHostEnvironment _environment;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ApiMetrics _metrics;

    public ProfileController(IWebHostEnvironment environment, UserManager<ApplicationUser> userManager, ApiMetrics metrics)
    {
        _environment = environment;
        _userManager = userManager;
        _metrics = metrics;
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
        var webRoot = string.IsNullOrWhiteSpace(_environment.WebRootPath)
            ? Path.Combine(_environment.ContentRootPath, "wwwroot")
            : _environment.WebRootPath;
        var uploadsFolder = Path.Combine(webRoot, "uploads", "profiles");
        Directory.CreateDirectory(uploadsFolder);

        var extension = Path.GetExtension(file.FileName);
        var fileName = $"{userId}_{Guid.NewGuid():N}{extension}";
        var fullPath = Path.Combine(uploadsFolder, fileName);

        await using (var stream = System.IO.File.Create(fullPath))
        {
            await file.CopyToAsync(stream);
        }

        var user = await _userManager.FindByIdAsync(userId);
        if (user is null)
        {
            return Unauthorized();
        }

        var publicPath = $"/uploads/profiles/{fileName}";
        user.ProfileImageUrl = $"{Request.Scheme}://{Request.Host}{publicPath}";
        await _userManager.UpdateAsync(user);
        stopwatch.Stop();
        _metrics.RecordUpload(TelemetryTags.Sections.ProfileImage, TelemetryTags.Outcomes.Success, 1, file.Length, stopwatch.Elapsed.TotalMilliseconds, userId);

        return Ok(new { imageUrl = user.ProfileImageUrl });
    }
}
