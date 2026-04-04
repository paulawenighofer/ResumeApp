using API.Data;
using API.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace API.Controllers;

[ApiController]
[Route("api/dashboard")]
public class DashboardController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly DashboardVisitStore _visitStore;
    private readonly AppLogStore _logStore;
    private readonly RequestStatsService _requestStats;

    public DashboardController(
        AppDbContext db,
        DashboardVisitStore visitStore,
        AppLogStore logStore,
        RequestStatsService requestStats)
    {
        _db = db;
        _visitStore = visitStore;
        _logStore = logStore;
        _requestStats = requestStats;
    }

    [HttpGet("summary")]
    public async Task<IActionResult> GetSummary()
    {
        var visits = await _visitStore.GetSnapshotAsync();
        var today = DateOnly.FromDateTime(DateTime.UtcNow).ToString("yyyy-MM-dd");

        var users = await _db.Users.CountAsync();
        var educations = await _db.Educations.CountAsync();
        var experiences = await _db.Experiences.CountAsync();
        var skills = await _db.Skills.CountAsync();
        var projects = await _db.Projects.CountAsync();
        var pendingOtps = await _db.OtpVerifications.CountAsync();

        var dailyLoads = visits.DailyLoads
            .OrderBy(x => x.Key)
            .TakeLast(14)
            .Select(x => new { date = x.Key, count = x.Value })
            .ToList();

        return Ok(new
        {
            generatedAtUtc = DateTime.UtcNow,
            cards = new
            {
                registeredUsers = users,
                resumeEntries = educations + experiences + skills + projects,
                pendingOtps,
                dashboardLoads = visits.TotalLoads,
                todayLoads = visits.DailyLoads.GetValueOrDefault(today, 0),
                apiRequestsSinceStartup = _requestStats.TotalRequests,
                failedRequestsSinceStartup = _requestStats.FailedRequests
            },
            sections = new[]
            {
                new { label = "Educations", value = educations },
                new { label = "Experiences", value = experiences },
                new { label = "Skills", value = skills },
                new { label = "Projects", value = projects }
            },
            dailyLoads
        });
    }

    [HttpGet("logs")]
    public IActionResult GetLogs()
    {
        return Ok(_logStore.GetRecent());
    }

    [HttpGet("log-stream")]
    public async Task GetLogStream(CancellationToken cancellationToken)
    {
        Response.Headers.Append("Content-Type", "text/event-stream");
        Response.Headers.Append("Cache-Control", "no-cache");

        long lastSeenId = 0;

        foreach (var entry in _logStore.GetRecent())
        {
            await WriteEventAsync(entry, cancellationToken);
            lastSeenId = entry.Id;
        }

        while (!cancellationToken.IsCancellationRequested)
        {
            var newEntries = _logStore.GetEntriesAfter(lastSeenId);
            if (newEntries.Count == 0)
            {
                await Response.WriteAsync(": heartbeat\n\n", cancellationToken);
                await Response.Body.FlushAsync(cancellationToken);
                await _logStore.WaitForNewEntriesAsync(cancellationToken);
                continue;
            }

            foreach (var entry in newEntries)
            {
                await WriteEventAsync(entry, cancellationToken);
                lastSeenId = entry.Id;
            }
        }
    }

    private async Task WriteEventAsync(DashboardLogEntry entry, CancellationToken cancellationToken)
    {
        var payload = System.Text.Json.JsonSerializer.Serialize(entry);
        await Response.WriteAsync($"data: {payload}\n\n", cancellationToken);
        await Response.Body.FlushAsync(cancellationToken);
    }
}
