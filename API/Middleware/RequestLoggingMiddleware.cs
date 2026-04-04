using System.Diagnostics;

namespace API.Middleware;

public class RequestLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestLoggingMiddleware> _logger;
    private readonly API.Services.RequestStatsService _requestStats;

    public RequestLoggingMiddleware(
        RequestDelegate next,
        ILogger<RequestLoggingMiddleware> logger,
        API.Services.RequestStatsService requestStats)
    {
        _next = next;
        _logger = logger;
        _requestStats = requestStats;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        _logger.LogInformation("Incoming request {Method} {Path}", context.Request.Method, context.Request.Path);
        var start = Stopwatch.GetTimestamp();

        try
        {
            await _next(context);
            var elapsedMs = Stopwatch.GetElapsedTime(start).TotalMilliseconds;
            _logger.LogInformation(
                "Completed request {Method} {Path} with status {StatusCode} in {ElapsedMs:0.000} ms",
                context.Request.Method,
                context.Request.Path,
                context.Response.StatusCode,
                elapsedMs);
            _requestStats.Record(context.Response.StatusCode);
        }
        catch (Exception ex)
        {
            var elapsedMs = Stopwatch.GetElapsedTime(start).TotalMilliseconds;
            _requestStats.Record(StatusCodes.Status500InternalServerError);
            _logger.LogError(
                ex,
                "Request failed {Method} {Path} in {ElapsedMs:0.000} ms",
                context.Request.Method,
                context.Request.Path,
                elapsedMs);
            throw;
        }
    }
}
