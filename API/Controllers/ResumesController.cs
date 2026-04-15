using API.Data;
using API.Models.Sync;
using API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using System.Security.Claims;

namespace API.Controllers;

[ApiController]
[Authorize]
[Route("api/resumes")]
public class ResumesController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ApiMetrics _metrics;
    private readonly IFileStorageService _fileStorage;

    public ResumesController(AppDbContext db, ApiMetrics metrics, IFileStorageService fileStorage)
    {
        _db = db;
        _metrics = metrics;
        _fileStorage = fileStorage;
    }

    [HttpPost("{id}/file")]
    [RequestSizeLimit(25_000_000)]
    public async Task<IActionResult> UploadFile(string id, IFormFile? file)
    {
        if (file is null || file.Length == 0)
        {
            _metrics.RecordUpload("resume-file", TelemetryTags.Outcomes.Failure, 0, 0, 0);
            return BadRequest(new { message = "No file uploaded." });
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized();
        }

        var resume = await _db.SyncResumes.FirstOrDefaultAsync(x => x.Id == id && x.UserId == userId);
        if (resume is null)
        {
            return NotFound();
        }

        var stopwatch = Stopwatch.StartNew();
        var extension = Path.GetExtension(file.FileName);
        var fileName = $"{id}_{Guid.NewGuid():N}{extension}";
        await using var stream = file.OpenReadStream();
        resume.PdfBlobUrl = await _fileStorage.SaveAsync(stream, fileName, Path.Combine("resumes", id), Request);
        resume.ResumeUpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        stopwatch.Stop();

        _metrics.RecordUpload("resume-file", TelemetryTags.Outcomes.Success, 1, file.Length, stopwatch.Elapsed.TotalMilliseconds, userId);
        return Ok(new { fileUrl = resume.PdfBlobUrl });
    }
}
