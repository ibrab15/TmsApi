using System.Diagnostics;

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
        // 1. Generate a short correlation ID
        string correlationId = Guid.NewGuid().ToString("N")[..8];

        // 2. Set the header EARLY (before next) so it's guaranteed to be sent
        context.Response.Headers["X-Correlation-Id"] = correlationId;

        // 3. Measure elapsed time and log entry
        var stopwatch = Stopwatch.StartNew();
        
        _logger.LogInformation(
            "Incoming Request: {Method} {Path} | CorrelationId: {CorrelationId}", 
            context.Request.Method, context.Request.Path, correlationId);

        // 4. Pass control to the next middleware in the pipeline
        await _next(context);

        // 5. After next completes, stop timing and log exit
        stopwatch.Stop();
        
        _logger.LogInformation(
            "Completed Request: {Method} {Path} -> Status: {StatusCode} in {ElapsedMs}ms | CorrelationId: {CorrelationId}", 
            context.Request.Method, context.Request.Path, context.Response.StatusCode, stopwatch.ElapsedMilliseconds, correlationId);
    }
}