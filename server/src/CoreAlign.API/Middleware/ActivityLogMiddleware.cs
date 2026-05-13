using System.Diagnostics;
using System.Security.Claims;
using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Interfaces;

namespace CoreAlign.API.Middleware;

public class ActivityLogMiddleware
{
    private static readonly string[] SkipMethods = { HttpMethods.Get, HttpMethods.Head, HttpMethods.Options };
    private static readonly string[] SkipPathPrefixes = { "/api/v1/auth", "/health", "/swagger" };

    private readonly RequestDelegate _next;

    public ActivityLogMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(
        HttpContext context,
        IActivityLogRepository activityLogRepository,
        IUnitOfWork unitOfWork,
        ILogger<ActivityLogMiddleware> logger)
    {
        if (!ShouldLog(context.Request))
        {
            await _next(context);
            return;
        }

        var stopwatch = Stopwatch.StartNew();
        await _next(context);
        stopwatch.Stop();

        var tenantClaim = context.User.FindFirstValue("tenant_id");
        if (!Guid.TryParse(tenantClaim, out var tenantId))
        {
            return;
        }

        Guid? userId = null;
        if (Guid.TryParse(context.User.FindFirstValue(ClaimTypes.NameIdentifier), out var parsedUserId))
        {
            userId = parsedUserId;
        }

        try
        {
            var log = new ActivityLog
            {
                TenantId = tenantId,
                UserId = userId,
                Method = context.Request.Method,
                Path = context.Request.Path.Value ?? string.Empty,
                StatusCode = context.Response.StatusCode,
                DurationMs = (int)stopwatch.ElapsedMilliseconds,
                IpAddress = context.Connection.RemoteIpAddress?.ToString(),
                UserAgent = context.Request.Headers.UserAgent.ToString(),
                TraceId = Activity.Current?.Id ?? context.TraceIdentifier
            };

            await activityLogRepository.AddAsync(log, context.RequestAborted);
            await unitOfWork.SaveChangesAsync(context.RequestAborted);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to persist activity log for {Method} {Path}", context.Request.Method, context.Request.Path);
        }
    }

    private static bool ShouldLog(HttpRequest request)
    {
        if (SkipMethods.Contains(request.Method, StringComparer.OrdinalIgnoreCase))
        {
            return false;
        }

        var path = request.Path.Value ?? string.Empty;
        return !SkipPathPrefixes.Any(prefix => path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));
    }
}
