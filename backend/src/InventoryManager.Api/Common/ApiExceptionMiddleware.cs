using System.Text.Json;
using Microsoft.AspNetCore.Mvc;

namespace InventoryManager.Api.Common;

/// <summary>
/// Translates domain exceptions into RFC 7807 problem responses so the API
/// returns consistent, machine-readable errors instead of raw stack traces.
/// </summary>
public class ApiExceptionMiddleware(RequestDelegate next, ILogger<ApiExceptionMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (NotFoundException ex)
        {
            await WriteProblem(context, StatusCodes.Status404NotFound, "Not Found", ex.Message);
        }
        catch (BusinessRuleException ex)
        {
            await WriteProblem(context, StatusCodes.Status409Conflict, "Business Rule Violation", ex.Message);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unhandled exception while processing {Path}", context.Request.Path);
            await WriteProblem(context, StatusCodes.Status500InternalServerError,
                "Server Error", "An unexpected error occurred.");
        }
    }

    private static async Task WriteProblem(HttpContext context, int status, string title, string detail)
    {
        var problem = new ProblemDetails { Status = status, Title = title, Detail = detail };
        context.Response.StatusCode = status;
        context.Response.ContentType = "application/problem+json";
        await context.Response.WriteAsync(JsonSerializer.Serialize(problem));
    }
}
