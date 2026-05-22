using System.Diagnostics;

public class TraceIdMiddleware
{
    private readonly RequestDelegate _next;

    public TraceIdMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(
        HttpContext context,
        ILogger<TraceIdMiddleware> logger)
    {
        var traceId = Activity.Current?.TraceId.ToString() ?? context.TraceIdentifier;
        context.Items["traceId"] = traceId;
        context.Response.Headers["X-Trace-Id"] = traceId;

        using var scope = logger.BeginScope(new Dictionary<string, object>
        {
            ["TraceId"] = traceId
        });

        await _next(context);
    }
}
