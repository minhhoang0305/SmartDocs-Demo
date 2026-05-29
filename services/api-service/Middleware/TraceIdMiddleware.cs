using System.Diagnostics;
using Serilog.Context;


public class TraceIdMiddleware
{
    private readonly RequestDelegate _next;

    public TraceIdMiddleware(RequestDelegate next)
    {
        _next = next;
    }
    public async Task InvokeAsync(HttpContext context)
    {
        var traceId = Activity.Current?.TraceId.ToString()?? context.TraceIdentifier;
        var spanId = Activity.Current?.SpanId.ToString();

        context.Items["traceId"] = traceId;
        context.Response.Headers["X-Trace-Id"] = traceId;

        using(LogContext.PushProperty("TraceId", traceId))
        using(LogContext.PushProperty("SpanId", spanId))
        {
            await _next(context);
        }
    }
}