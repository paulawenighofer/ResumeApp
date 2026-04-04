using System.Diagnostics;

namespace API.Middleware;

public class RequestLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestLoggingMiddleware> _logger;

    public RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (context.Request.Path.StartsWithSegments("/metrics"))
        {
            await _next(context);
            return;
        }

        _logger.LogInformation("Incoming request {Method} {Path}", context.Request.Method, context.Request.Path);
        var start = Stopwatch.GetTimestamp();

        try
        {
            await _next(context);
            var elapsedMs = Stopwatch.GetElapsedTime(start).TotalMilliseconds;
            var status = context.Response.StatusCode;

            if (status >= 500)
                _logger.LogError(
                    "Request failed {Method} {Path} with status {StatusCode} in {ElapsedMs:0.000} ms",
                    context.Request.Method, context.Request.Path, status, elapsedMs);
            else if (status >= 400)
                _logger.LogWarning(
                    "Request rejected {Method} {Path} with status {StatusCode} in {ElapsedMs:0.000} ms",
                    context.Request.Method, context.Request.Path, status, elapsedMs);
            else
                _logger.LogInformation(
                    "Completed request {Method} {Path} with status {StatusCode} in {ElapsedMs:0.000} ms",
                    context.Request.Method, context.Request.Path, status, elapsedMs);
        }
        catch (Exception ex)
        {
            var elapsedMs = Stopwatch.GetElapsedTime(start).TotalMilliseconds;
            _logger.LogError(
                ex,
                "Unhandled exception {Method} {Path} in {ElapsedMs:0.000} ms",
                context.Request.Method,
                context.Request.Path,
                elapsedMs);
            throw;
        }
    }
}
