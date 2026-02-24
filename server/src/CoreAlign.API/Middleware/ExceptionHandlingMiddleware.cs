using System.Text.Json;
using CoreAlign.Application.Common;
using CoreAlign.Domain.Exceptions;
using FluentValidation;

namespace CoreAlign.API.Middleware;

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
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var (statusCode, errors) = exception switch
        {
            DomainException domainEx => (domainEx.StatusCode, new List<string> { domainEx.Message }),
            ValidationException validationEx => (400, validationEx.Errors.Select(e => e.ErrorMessage).ToList()),
            UnauthorizedAccessException => (401, new List<string> { "Unauthorized access." }),
            _ => (500, new List<string> { "An unexpected error occurred." })
        };

        if (statusCode == 500)
        {
            _logger.LogError(exception, "Unhandled exception occurred");
        }

        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/json";

        var response = ApiResponse<object>.Failure(errors, statusCode);

        var jsonOptions = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
        await context.Response.WriteAsync(JsonSerializer.Serialize(response, jsonOptions));
    }
}
