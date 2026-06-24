using System.Diagnostics;
using System.Security.Claims;
using System.Text.Json;
using CoreAlign.Application.Common;
using CoreAlign.Application.Common.Observability;
using CoreAlign.Domain.Enums;
using CoreAlign.Domain.Exceptions;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace CoreAlign.API.Middleware;

public class ExceptionHandlingMiddleware
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
    };

    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;
    private readonly IErrorLogWriter _errorLogWriter;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger, IErrorLogWriter errorLogWriter)
    {
        _next = next;
        _logger = logger;
        _errorLogWriter = errorLogWriter;
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
        var correlationId = context.Items.TryGetValue(CorrelationIdMiddleware.ItemsKey, out var cid) && cid is string s
            ? s
            : null;
        var traceId = correlationId ?? Activity.Current?.Id ?? context.TraceIdentifier;

        var (statusCode, errors) = exception switch
        {
            NotFoundException notFoundEx => (StatusCodes.Status404NotFound, new List<string> { notFoundEx.Message }),
            ConflictException conflictEx => (StatusCodes.Status409Conflict, new List<string> { conflictEx.Message }),
            AuthenticationException authEx => (StatusCodes.Status401Unauthorized, new List<string> { authEx.Message }),
            ForbiddenException forbidEx => (StatusCodes.Status403Forbidden, new List<string> { forbidEx.Message }),
            RateLimitExceededException rateEx => (StatusCodes.Status429TooManyRequests, new List<string> { rateEx.Message }),
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
        if (exception is ValidationException validation)
        {
            response.FieldErrors = validation.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).Distinct().ToArray());
        }
        // Stream JSON directly to the response body — avoids the intermediate
        // string allocation + UTF-16→UTF-8 re-encoding that the old
        // Serialize→WriteAsync path performed.
        await JsonSerializer.SerializeAsync(context.Response.Body, response, JsonOptions, context.RequestAborted);

        if (ShouldCapture(statusCode, exception))
        {
            await CaptureAsync(context, exception, statusCode, traceId);
        }
    }

    // Persist 5xx always (Error) and meaningful 4xx (Warning), but skip the high-volume
    // expected ones: FluentValidation user-input failures, 401 auth challenges, 404 misses.
    private static bool ShouldCapture(int statusCode, Exception exception)
    {
        if (statusCode >= 500) return true;
        if (exception is ValidationException or AuthenticationException or NotFoundException) return false;
        return statusCode is not (StatusCodes.Status401Unauthorized or StatusCodes.Status404NotFound);
    }

    private async Task CaptureAsync(HttpContext context, Exception exception, int statusCode, string traceId)
    {
        var user = context.User;
        Guid? tenantId = TryParseGuid(user.FindFirstValue("tenant_id"));
        Guid? userId = TryParseGuid(user.FindFirstValue(ClaimTypes.NameIdentifier) ?? user.FindFirstValue("sub"));
        var userName = user.FindFirstValue(ClaimTypes.Name) ?? user.FindFirstValue("email") ?? user.Identity?.Name;

        var record = new ErrorLogRecord(
            CorrelationId: traceId,
            Source: ErrorSource.Backend,
            Severity: statusCode >= 500 ? ErrorSeverity.Error : ErrorSeverity.Warning,
            Message: exception.GetBaseException().Message,
            TraceId: Activity.Current?.Id,
            StatusCode: statusCode,
            HttpMethod: context.Request.Method,
            Path: context.Request.Path.Value,
            ExceptionType: exception.GetType().FullName,
            StackTrace: exception.ToString(),
            TenantId: tenantId,
            UserId: userId,
            UserName: userName,
            UserAgent: context.Request.Headers.UserAgent.ToString());

        await _errorLogWriter.WriteAsync(record, CancellationToken.None);
    }

    private static Guid? TryParseGuid(string? value) =>
        Guid.TryParse(value, out var g) ? g : null;
}
