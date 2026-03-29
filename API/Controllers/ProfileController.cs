using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Shared.Models;
using System.Security.Claims;

namespace API.Controllers;

[ApiController]
[Authorize]
[Route("api/profile")]
public class ProfileController : ControllerBase
{
    private readonly IWebHostEnvironment _environment;
    private readonly UserManager<ApplicationUser> _userManager;

    public ProfileController(IWebHostEnvironment environment, UserManager<ApplicationUser> userManager)
    {
        _environment = environment;
        _userManager = userManager;
    }

    [HttpPost("image")]
    [RequestSizeLimit(10_000_000)]
    public async Task<IActionResult> UploadProfileImage(IFormFile? file)
    {
        if (file is null || file.Length == 0)
        {
            return BadRequest(new { message = "No image uploaded." });
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized();
        }

        var uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads", "profiles");
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

        return Ok(new { imageUrl = user.ProfileImageUrl });
    }
}
