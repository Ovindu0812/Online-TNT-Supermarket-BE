using System.Net;
using System.Text.Json;
using FluentValidation;
using TNT.IdentityService.Api.Exceptions;

namespace TNT.IdentityService.Api.Middleware;

/// <summary>
/// Global exception handling middleware.
/// Returns consistent ProblemDetails-style JSON responses.
/// Never exposes stack traces, connection strings, or secrets in production.
/// </summary>
public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;
    private readonly IHostEnvironment _env;

    public ExceptionHandlingMiddleware(
        RequestDelegate next,
        ILogger<ExceptionHandlingMiddleware> logger,
        IHostEnvironment env)
    {
        _next = next;
        _logger = logger;
        _env = env;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (ConflictException ex)
        {
            _logger.LogWarning("Conflict: {Message}", ex.Message);
            await WriteErrorAsync(context, HttpStatusCode.Conflict, "Conflict", ex.Message);
        }
        catch (UnauthorizedException ex)
        {
            _logger.LogWarning("Unauthorized: {Message}", ex.Message);
            await WriteErrorAsync(context, HttpStatusCode.Unauthorized, "Unauthorized", ex.Message);
        }
        catch (NotFoundException ex)
        {
            _logger.LogWarning("Not Found: {Message}", ex.Message);
            await WriteErrorAsync(context, HttpStatusCode.NotFound, "Not Found", ex.Message);
        }
        catch (ValidationException ex)
        {
            var errors = ex.Errors.Select(e => e.ErrorMessage).ToList();
            _logger.LogWarning("Validation failed: {Errors}", string.Join("; ", errors));
            await WriteValidationErrorAsync(context, errors);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception. TraceId={TraceId}", context.TraceIdentifier);

            var detail = _env.IsDevelopment() || _env.IsEnvironment("Testing")
                ? ex.Message
                : "An unexpected error occurred. Please try again later.";

            await WriteErrorAsync(context, HttpStatusCode.InternalServerError, "Internal Server Error", detail);
        }
    }

    private static async Task WriteErrorAsync(
        HttpContext context, HttpStatusCode statusCode, string title, string detail)
    {
        context.Response.StatusCode = (int)statusCode;
        context.Response.ContentType = "application/problem+json";
        var body = JsonSerializer.Serialize(new
        {
            status = (int)statusCode,
            title,
            detail,
            traceId = context.TraceIdentifier
        });
        await context.Response.WriteAsync(body);
    }

    private static async Task WriteValidationErrorAsync(HttpContext context, List<string> errors)
    {
        context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
        context.Response.ContentType = "application/problem+json";
        var body = JsonSerializer.Serialize(new
        {
            status = 400,
            title = "Validation failed",
            detail = "One or more validation errors occurred.",
            errors,
            traceId = context.TraceIdentifier
        });
        await context.Response.WriteAsync(body);
    }
}
