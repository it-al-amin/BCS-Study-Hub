using Microsoft.AspNetCore.Mvc;
using BCSStudyHub.Application.Syllabus;

namespace BCSStudyHub.Api.Middleware;

public sealed class ExceptionHandlingMiddleware(
    RequestDelegate next,
    ILogger<ExceptionHandlingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (SyllabusException exception)
        {
            if (context.Response.HasStarted)
            {
                throw;
            }

            logger.LogInformation(
                "Syllabus request rejected with {StatusCode}: {Detail}",
                exception.StatusCode,
                exception.Message);
            context.Response.Clear();
            context.Response.StatusCode = exception.StatusCode;
            context.Response.ContentType = "application/problem+json";
            await context.Response.WriteAsJsonAsync(new ProblemDetails
            {
                Status = exception.StatusCode,
                Title = exception.Title,
                Detail = exception.Message,
            });
        }
        catch (Exception exception)
        {
            if (context.Response.HasStarted)
            {
                throw;
            }

            logger.LogError(
                exception,
                "Unhandled exception processing {Method} {Path}. Correlation ID: {CorrelationId}",
                context.Request.Method,
                context.Request.Path,
                context.TraceIdentifier);

            context.Response.Clear();
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            context.Response.ContentType = "application/problem+json";
            await context.Response.WriteAsJsonAsync(new ProblemDetails
            {
                Status = StatusCodes.Status500InternalServerError,
                Title = "An unexpected error occurred.",
                Detail = "The request could not be completed. Use the correlation ID when contacting support.",
                Extensions = { ["traceId"] = context.TraceIdentifier },
            });
        }
    }
}
