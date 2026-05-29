using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

public static class ExceptionMiddleware
{
    public static void ConfigureExceptionHandler(this IApplicationBuilder app)
    {
        app.UseExceptionHandler(errorApp =>
        {
            errorApp.Run(async context =>
            {
                var exceptionFeature = context.Features.Get<IExceptionHandlerFeature>();
                var exception = exceptionFeature?.Error;

                var statusCode = exception switch
                {
                    ArgumentException => StatusCodes.Status400BadRequest,
                    BadHttpRequestException => StatusCodes.Status400BadRequest,
                    UnauthorizedAccessException => StatusCodes.Status401Unauthorized,
                    KeyNotFoundException => StatusCodes.Status404NotFound,
                    _ => StatusCodes.Status500InternalServerError
                };

                var problem = new ProblemDetails
                {
                    Status = statusCode,
                    Title = statusCode == StatusCodes.Status500InternalServerError
                        ? "Internal server error"
                        : "Request failed",
                    Detail = statusCode == StatusCodes.Status500InternalServerError
                        ? "An unexpected error occurred."
                        : exception?.Message,
                    Instance = context.Request.Path
                };

                problem.Extensions["traceId"] = context.Items["traceId"]?.ToString()??Activity.Current?.TraceId.ToString() ?? context.TraceIdentifier;

                context.Response.StatusCode = statusCode;
                context.Response.ContentType = "application/problem+json";

                await context.Response.WriteAsJsonAsync(problem);
            });
        });
    }
}