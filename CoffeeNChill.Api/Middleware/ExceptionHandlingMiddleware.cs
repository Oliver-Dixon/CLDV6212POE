namespace CoffeeNChill.Api.Middleware;

/// <summary>
/// Catches anything that escapes a controller and returns consistent JSON
/// instead of an HTML stack trace.
/// </summary>
public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _log;

    public ExceptionHandlingMiddleware(
        RequestDelegate next, ILogger<ExceptionHandlingMiddleware> log)
    {
        _next = next;
        _log = log;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (HttpRequestException ex)
        {
            _log.LogError(ex, "Backend unreachable for {Path}", context.Request.Path);
            await WriteAsync(context, StatusCodes.Status503ServiceUnavailable,
                "The backend service is unavailable. Please try again shortly.");
        }
        catch (TaskCanceledException ex)
        {
            _log.LogError(ex, "Backend timed out for {Path}", context.Request.Path);
            await WriteAsync(context, StatusCodes.Status504GatewayTimeout,
                "The backend service did not respond in time.");
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "Unhandled exception for {Path}", context.Request.Path);
            await WriteAsync(context, StatusCodes.Status500InternalServerError,
                "An unexpected error occurred.");
        }
    }

    private static Task WriteAsync(HttpContext context, int status, string message)
    {
        if (context.Response.HasStarted) return Task.CompletedTask;

        context.Response.Clear();
        context.Response.StatusCode = status;
        context.Response.ContentType = "application/problem+json";

        return context.Response.WriteAsJsonAsync(new
        {
            status,
            error = message,
            traceId = context.TraceIdentifier,
            timestamp = DateTimeOffset.UtcNow
        });
    }
}