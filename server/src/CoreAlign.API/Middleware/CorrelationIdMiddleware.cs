using System.Diagnostics;
using Serilog.Context;

namespace CoreAlign.API.Middleware;

public class CorrelationIdMiddleware
{
    public const string HeaderName = "X-Correlation-Id";
    public const string ItemsKey = "CorrelationId";

    private readonly RequestDelegate _next;

    public CorrelationIdMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = context.Request.Headers[HeaderName].FirstOrDefault();
        if (string.IsNullOrWhiteSpace(correlationId) || !Guid.TryParse(correlationId, out _))
        {
            correlationId = Guid.NewGuid().ToString("N");
        }

        context.Response.OnStarting(() =>
        {
            if (!context.Response.Headers.ContainsKey(HeaderName))
            {
                context.Response.Headers[HeaderName] = correlationId;
            }
            return Task.CompletedTask;
        });

        Activity.Current?.SetTag("correlation_id", correlationId);
        Activity.Current?.SetBaggage("correlation.id", correlationId);
        Sentry.SentrySdk.ConfigureScope(scope => scope.SetTag("correlation_id", correlationId));

        using (LogContext.PushProperty("CorrelationId", correlationId))
        {
            context.Items[ItemsKey] = correlationId;
            await _next(context);
        }
    }
}
