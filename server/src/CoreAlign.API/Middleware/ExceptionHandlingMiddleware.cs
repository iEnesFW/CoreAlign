using System.Diagnostics;
using System.Text.Json;
using CoreAlign.Application.Common;
using CoreAlign.Domain.Exceptions;
using FluentValidation;

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
            _ => (StatusCodes.Status500InternalServerError, new List<string> { "An unexpected error occurred." })
        };

        if (statusCode >= 500)
        {
            _logger.LogError(exception, "Unhandled exception. TraceId: {TraceId}", traceId);
        }
        else if (statusCode >= 400)
        {
            _logger.LogWarning("Handled exception {ExceptionType}. TraceId: {TraceId}. Message: {Message}",
                exception.GetType().Name, traceId, exception.Message);
        }

        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/json";

        var response = ApiResponse<object>.Failure(errors, statusCode, traceId);
        await context.Response.WriteAsync(JsonSerializer.Serialize(response, JsonOptions));
    }
}
