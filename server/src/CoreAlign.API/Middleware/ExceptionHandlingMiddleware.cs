using System.Diagnostics;
using System.Text.Json;
using CoreAlign.Application.Common;
using CoreAlign.Domain.Exceptions;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace CoreAlign.API.Middleware;

public class ExceptionHandlingMiddleware
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

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
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var traceId = Activity.Current?.Id ?? context.TraceIdentifier;

        var (statusCode, errors) = exception switch
        {
            NotFoundException notFoundEx => (StatusCodes.Status404NotFound, new List<string> { notFoundEx.Message }),
            ConflictException conflictEx => (StatusCodes.Status409Conflict, new List<string> { conflictEx.Message }),
            AuthenticationException authEx => (StatusCodes.Status401Unauthorized, new List<string> { authEx.Message }),
            ForbiddenException forbidEx => (StatusCodes.Status403Forbidden, new List<string> { forbidEx.Message }),
            DomainException domainEx => (StatusCodes.Status400BadRequest, new List<string> { domainEx.Message }),
            ValidationException validationEx => (StatusCodes.Status400BadRequest, validationEx.Errors.Select(e => e.ErrorMessage).Distinct().ToList()),
            UnauthorizedAccessException => (StatusCodes.Status401Unauthorized, new List<string> { "Unauthorized." }),
            DbUpdateException { InnerException: PostgresException { SqlState: PostgresErrorCodes.UniqueViolation } } => (StatusCodes.Status409Conflict, new List<string> { "A record with the same unique value already exists." }),
            DbUpdateException { InnerException: PostgresException { SqlState: PostgresErrorCodes.ForeignKeyViolation } } => (StatusCodes.Status409Conflict, new List<string> { "A referenced record does not exist or is still in use." }),
            DbUpdateConcurrencyException => (StatusCodes.Status409Conflict, new List<string> { "The record was modified by another operation. Reload and retry." }),
            _ => (StatusCodes.Status500InternalServerError, new List<string> { "An unexpected error occurred." })
        };

        if (statusCode >= 500)
        {
            _logger.LogError(exception, "Unhandled exception. TraceId: {TraceId}", traceId);
        }
        else if (statusCode >= 400)
        {
            // Note: only log the exception *type* and trace id at warning level.
            // Message text may contain user-supplied or schema details (FluentValidation
            // interpolates field values into errors) — keep that out of plain warning logs.
            _logger.LogWarning(
                "Handled exception {ExceptionType} → {StatusCode}. TraceId: {TraceId}",
                exception.GetType().Name,
                statusCode,
                traceId);
        }

        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/json";

        var response = ApiResponse<object>.Failure(errors, statusCode, traceId);
        // Stream JSON directly to the response body — avoids the intermediate
        // string allocation + UTF-16→UTF-8 re-encoding that the old
        // Serialize→WriteAsync path performed.
        await JsonSerializer.SerializeAsync(context.Response.Body, response, JsonOptions, context.RequestAborted);
    }
}
