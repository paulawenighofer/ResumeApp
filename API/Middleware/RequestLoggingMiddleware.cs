using System.Diagnostics;
using System.Security.Claims;
using API.Services;
using Microsoft.AspNetCore.Routing;

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

    public async Task InvokeAsync(HttpContext context, ApiMetrics metrics)
    {
        var path = context.Request.Path;

        if (path.StartsWithSegments("/metrics"))
        {
            await _next(context);
            return;
        }

        var isHealthEndpoint = path.StartsWithSegments("/health");
        var userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier);

        // Health probes happen every second — suppress the incoming-request line to avoid log flooding
        if (!isHealthEndpoint)
            _logger.LogInformation(
                "Incoming request {Method} {Path} {UserId}",
                context.Request.Method,
                path,
                userId ?? "anonymous");

        var start = Stopwatch.GetTimestamp();

        try
        {
            await _next(context);
            var elapsedMs = Stopwatch.GetElapsedTime(start).TotalMilliseconds;
            var status = context.Response.StatusCode;
            RecordRequestMetric(context, metrics, status, elapsedMs);

            // Suppress completion log for successful health probes; still log failures
            if (isHealthEndpoint && status < 400)
                return;

            if (status >= 500)
                _logger.LogError(
                    "Request failed {Method} {Path} with status {StatusCode} in {ElapsedMs:0.000} ms {UserId}",
                    context.Request.Method, path, status, elapsedMs, userId ?? "anonymous");
            else if (status >= 400)
                _logger.LogWarning(
                    "Request rejected {Method} {Path} with status {StatusCode} in {ElapsedMs:0.000} ms {UserId}",
                    context.Request.Method, path, status, elapsedMs, userId ?? "anonymous");
            else
                _logger.LogInformation(
                    "Completed request {Method} {Path} with status {StatusCode} in {ElapsedMs:0.000} ms {UserId}",
                    context.Request.Method, path, status, elapsedMs, userId ?? "anonymous");
        }
        catch (Exception ex)
        {
            var elapsedMs = Stopwatch.GetElapsedTime(start).TotalMilliseconds;
            RecordRequestMetric(context, metrics, StatusCodes.Status500InternalServerError, elapsedMs);
            _logger.LogError(
                ex,
                "Unhandled exception {Method} {Path} in {ElapsedMs:0.000} ms {UserId}",
                context.Request.Method,
                path,
                elapsedMs,
                userId ?? "anonymous");
            throw;
        }
    }

    private static void RecordRequestMetric(HttpContext context, ApiMetrics metrics, int statusCode, double durationMs)
    {
        if (!ShouldTrackRequest(context))
        {
            return;
        }

        metrics.RecordHttpRequest(
            context.Request.Scheme,
            context.Request.Method,
            GetNormalizedRoute(context),
            statusCode,
            durationMs);
    }

    private static bool ShouldTrackRequest(HttpContext context)
    {
        var path = context.Request.Path;
        return path.StartsWithSegments("/api", StringComparison.OrdinalIgnoreCase)
               && !path.StartsWithSegments("/api/openapi", StringComparison.OrdinalIgnoreCase)
               && !path.StartsWithSegments("/api/scalar", StringComparison.OrdinalIgnoreCase);
    }

    private static string GetNormalizedRoute(HttpContext context)
    {
        if (context.GetEndpoint() is RouteEndpoint routeEndpoint
            && !string.IsNullOrWhiteSpace(routeEndpoint.RoutePattern.RawText))
        {
            return $"/{routeEndpoint.RoutePattern.RawText.TrimStart('/')}";
        }

        return string.IsNullOrWhiteSpace(context.Request.Path.Value)
            ? "/api"
            : context.Request.Path.Value!;
    }
}
