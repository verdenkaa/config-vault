using ConfigVault.Api.Exceptions;
using System.Net;
using System.Text.Json;

namespace ConfigVault.Api.Middleware;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (NotFoundException ex)
        {
            await HandleExceptionAsync(context, 404, ex.Message);
        }
        catch (AccessDeniedException ex)
        {
            await HandleExceptionAsync(context, 403, ex.Message);
        }
        catch (Exceptions.ValidationException ex)
        {
            await HandleExceptionAsync(context, 400, ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception");
            await HandleExceptionAsync(context, 500, "Внутренняя ошибка сервера.");
        }
    }

    private static async Task HandleExceptionAsync(HttpContext context, int statusCode, string message)
    {
        context.Response.ContentType = "application/json";
        context.Response.StatusCode = statusCode;
        var errorResponse = new { message };
        await context.Response.WriteAsync(JsonSerializer.Serialize(errorResponse));
    }
}