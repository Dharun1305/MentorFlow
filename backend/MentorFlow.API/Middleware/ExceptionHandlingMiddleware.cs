using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.SqlClient;
using MentorFlow.API.DTOs;
using MentorFlow.API.Services;

namespace MentorFlow.API.Middleware;
public sealed class ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (ApiException exception)
        {
            await WriteErrorAsync(context, exception.StatusCode, exception.Message);
        }
        catch (DbUpdateConcurrencyException exception)
        {
            logger.LogWarning(exception, "A record was changed by another request.");
            await WriteErrorAsync(context, StatusCodes.Status409Conflict, "This record has changed. Reload it before saving again.");
        }
        catch (OperationCanceledException)when (context.RequestAborted.IsCancellationRequested)
        {
        }
        catch (DbUpdateException exception)when (exception.InnerException is SqlException sql && sql.Number is 2601 or 2627 or 547)
        {
            logger.LogWarning(exception, "A database update failed.");
            await WriteErrorAsync(context, StatusCodes.Status409Conflict, "The requested change conflicts with existing data.");
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "An unhandled API exception occurred.");
            await WriteErrorAsync(context, StatusCodes.Status500InternalServerError, "An unexpected error occurred.");
        }
    }

    private static async Task WriteErrorAsync(HttpContext context, int statusCode, string message)
    {
        if (context.Response.HasStarted)
            return;
        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsync(JsonSerializer.Serialize(new ErrorResponse(message), new JsonSerializerOptions(JsonSerializerDefaults.Web)));
    }
}
