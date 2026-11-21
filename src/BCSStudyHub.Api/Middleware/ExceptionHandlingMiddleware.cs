using Microsoft.AspNetCore.Mvc;
using BCSStudyHub.Application.Syllabus;
using BCSStudyHub.Application.Mcq;
using BCSStudyHub.Application.Written;
using BCSStudyHub.Application.Viva;
using BCSStudyHub.Application.Library;
using BCSStudyHub.Application.CurrentAffairs;
using BCSStudyHub.Application.Planner;
using BCSStudyHub.Application.Admin;

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
            }, (System.Text.Json.JsonSerializerOptions?)null, "application/problem+json");
        }
        catch (McqException exception)
        {
            if (context.Response.HasStarted)
            {
                throw;
            }

            logger.LogInformation(
                "MCQ request rejected with {StatusCode}: {Detail}",
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
            }, (System.Text.Json.JsonSerializerOptions?)null, "application/problem+json");
        }
        catch (WrittenException exception)
        {
            if (context.Response.HasStarted)
            {
                throw;
            }

            logger.LogInformation(
                "Written request rejected with {StatusCode}: {Detail}",
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
            }, (System.Text.Json.JsonSerializerOptions?)null, "application/problem+json");
        }
        catch (VivaException exception)
        {
            if (context.Response.HasStarted)
            {
                throw;
            }

            logger.LogInformation(
                "Viva request rejected with {StatusCode}: {Detail}",
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
            }, (System.Text.Json.JsonSerializerOptions?)null, "application/problem+json");
        }
        catch (LibraryException exception)
        {
            if (context.Response.HasStarted)
            {
                throw;
            }

            logger.LogInformation(
                "Library request rejected with {StatusCode}: {Detail}",
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
            }, (System.Text.Json.JsonSerializerOptions?)null, "application/problem+json");
        }
        catch (AdminException exception)
        {
            if (context.Response.HasStarted)
            {
                throw;
            }

            logger.LogInformation("Admin request rejected with {StatusCode}: {Detail}", exception.StatusCode, exception.Message);
            context.Response.Clear();
            context.Response.StatusCode = exception.StatusCode;
            context.Response.ContentType = "application/problem+json";
            await context.Response.WriteAsJsonAsync(new ProblemDetails
            {
                Status = exception.StatusCode,
                Title = exception.Title,
                Detail = exception.Message,
            }, (System.Text.Json.JsonSerializerOptions?)null, "application/problem+json");
        }
        catch (PlannerException exception)
        {
            if (context.Response.HasStarted)
            {
                throw;
            }

            logger.LogInformation("Planner request rejected with {StatusCode}: {Detail}", exception.StatusCode, exception.Message);
            context.Response.Clear();
            context.Response.StatusCode = exception.StatusCode;
            context.Response.ContentType = "application/problem+json";
            await context.Response.WriteAsJsonAsync(new ProblemDetails
            {
                Status = exception.StatusCode,
                Title = exception.Title,
                Detail = exception.Message,
            }, (System.Text.Json.JsonSerializerOptions?)null, "application/problem+json");
        }
        catch (CurrentAffairsException exception)
        {
            if (context.Response.HasStarted)
            {
                throw;
            }

            logger.LogInformation(
                "Current-affairs request rejected with {StatusCode}: {Detail}",
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
            }, (System.Text.Json.JsonSerializerOptions?)null, "application/problem+json");
        }
        catch (BadHttpRequestException exception) when (exception.StatusCode == StatusCodes.Status413PayloadTooLarge)
        {
            if (context.Response.HasStarted)
            {
                throw;
            }

            context.Response.Clear();
            context.Response.StatusCode = StatusCodes.Status413PayloadTooLarge;
            context.Response.ContentType = "application/problem+json";
            await context.Response.WriteAsJsonAsync(new ProblemDetails
            {
                Status = StatusCodes.Status413PayloadTooLarge,
                Title = "Request too large",
                Detail = "The uploaded file exceeds the maximum accepted size.",
            }, (System.Text.Json.JsonSerializerOptions?)null, "application/problem+json");
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
            }, (System.Text.Json.JsonSerializerOptions?)null, "application/problem+json");
        }
    }
}
