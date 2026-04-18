using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Shared.Models;
using System.Diagnostics;
using System.Security.Claims;
using API.Services;

namespace API.Controllers;

[ApiController]
[Authorize]
[Route("api/profile")]
public class ProfileController : ControllerBase
{
    private const long MaxProfileImageBytes = 10_000_000;

    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg",
        ".jpeg",
        ".png",
        ".gif",
        ".webp"
    };

    private static readonly HashSet<string> AllowedContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/jpeg",
        "image/png",
        "image/gif",
        "image/webp"
    };

    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ApiMetrics _metrics;
    private readonly IBlobStorageService _blobStorageService;
    private readonly ILogger<ProfileController> _logger;

    public ProfileController(
        UserManager<ApplicationUser> userManager,
        ApiMetrics metrics,
        IBlobStorageService blobStorageService,
        ILogger<ProfileController> logger)
    {
        _userManager = userManager;
        _metrics = metrics;
        _blobStorageService = blobStorageService;
        _logger = logger;
    }

    [HttpPost("image")]
    [RequestSizeLimit(MaxProfileImageBytes)]
    public async Task<IActionResult> UploadProfileImage(IFormFile? file, CancellationToken cancellationToken)
    {
        if (file is null || file.Length == 0)
        {
            _metrics.RecordUpload(TelemetryTags.Sections.ProfileImage, TelemetryTags.Outcomes.Failure, 0, 0, 0);
            return BadRequest(new { message = "No image uploaded." });
        }

        if (file.Length > MaxProfileImageBytes)
        {
            _metrics.RecordUpload(TelemetryTags.Sections.ProfileImage, TelemetryTags.Outcomes.Failure, 1, file.Length, 0);
            return BadRequest(new { message = "Image is too large. Maximum size is 10 MB." });
        }

        var extension = Path.GetExtension(file.FileName);
        if (string.IsNullOrWhiteSpace(extension) || !AllowedExtensions.Contains(extension))
        {
            _metrics.RecordUpload(TelemetryTags.Sections.ProfileImage, TelemetryTags.Outcomes.Failure, 1, file.Length, 0);
            return BadRequest(new { message = "Unsupported image type." });
        }

        if (string.IsNullOrWhiteSpace(file.ContentType) || !AllowedContentTypes.Contains(file.ContentType))
        {
            _metrics.RecordUpload(TelemetryTags.Sections.ProfileImage, TelemetryTags.Outcomes.Failure, 1, file.Length, 0);
            return BadRequest(new { message = "Unsupported image content type." });
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized();
        }

        var user = await _userManager.FindByIdAsync(userId);
        if (user is null)
        {
            return Unauthorized();
        }

        var stopwatch = Stopwatch.StartNew();

        try
        {
            await using var uploadStream = file.OpenReadStream();
            var uploadedImageUrl = await _blobStorageService.UploadProfileImageAsync(
                userId,
                uploadStream,
                extension,
                file.ContentType,
                cancellationToken);

            var previousImageUrl = user.ProfileImageUrl;
            user.ProfileImageUrl = uploadedImageUrl;

            var updateResult = await _userManager.UpdateAsync(user);
            if (!updateResult.Succeeded)
            {
                _logger.LogWarning("Failed to update ProfileImageUrl for user {UserId}: {Errors}",
                    userId,
                    string.Join("; ", updateResult.Errors.Select(e => e.Description)));

                _metrics.RecordUpload(TelemetryTags.Sections.ProfileImage, TelemetryTags.Outcomes.Failure, 1, file.Length, stopwatch.Elapsed.TotalMilliseconds, userId);
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Profile image upload failed." });
            }

            if (!string.IsNullOrWhiteSpace(previousImageUrl) &&
                !string.Equals(previousImageUrl, uploadedImageUrl, StringComparison.OrdinalIgnoreCase))
            {
                await _blobStorageService.TryDeleteAsync(previousImageUrl, cancellationToken);
            }

            stopwatch.Stop();
            _logger.LogInformation("Profile image uploaded successfully for user {UserId}: {ImageUrl}", userId, uploadedImageUrl);
            _metrics.RecordUpload(TelemetryTags.Sections.ProfileImage, TelemetryTags.Outcomes.Success, 1, file.Length, stopwatch.Elapsed.TotalMilliseconds, userId);

            return Ok(new { imageUrl = user.ProfileImageUrl });
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "Profile image upload failed for user {UserId}", userId);
            _metrics.RecordUpload(TelemetryTags.Sections.ProfileImage, TelemetryTags.Outcomes.Failure, 1, file.Length, stopwatch.Elapsed.TotalMilliseconds, userId);
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Profile image upload failed." });
        }
    }
}
