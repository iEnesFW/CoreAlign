using CoreAlign.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;
using DiagActivity = System.Diagnostics.Activity;
using DiagStopwatch = System.Diagnostics.Stopwatch;

namespace CoreAlign.Application.Common.Behaviors;

/// <summary>
/// Pipeline-wide structured logging for every MediatR request.
///
/// Emits one Information log when a request completes, with:
///   • request type name (handler entry point)
///   • elapsed milliseconds
///   • tenant id (if any) — useful for tenant-scoped diagnosis
///   • a marker indicating success vs. failure
///
/// Log scope wraps the handler invocation so any inner ILogger calls inherit
/// the same RequestType/TenantId/RequestId fields automatically — handlers
/// don't need to repeat them.
/// </summary>
public class LoggingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly ILogger<LoggingBehavior<TRequest, TResponse>> _logger;
    private readonly ITenantContext _tenantContext;

    public LoggingBehavior(
        ILogger<LoggingBehavior<TRequest, TResponse>> logger,
        ITenantContext tenantContext)
    {
        _logger = logger;
        _tenantContext = tenantContext;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        // Cheap path: when neither Information nor Debug is enabled, skip scope
        // allocation entirely. The scope dictionary is per-request overhead that
        // disappears in prod when log level is Warning or higher.
        var traceEnabled = _logger.IsEnabled(LogLevel.Information) || _logger.IsEnabled(LogLevel.Debug);
        if (!traceEnabled)
        {
            return await next();
        }

        var requestType = typeof(TRequest).Name;
        var tenantId = _tenantContext.CurrentTenantId;

        using var scope = _logger.BeginScope(new Dictionary<string, object?>
        {
            ["RequestType"] = requestType,
            ["TenantId"] = tenantId,
            ["RequestId"] = DiagActivity.Current?.Id,
        });

        var sw = DiagStopwatch.StartNew();
        try
        {
            var response = await next();
            sw.Stop();

            // Only chatter at debug for fast read queries to keep prod logs lean;
            // anything > 500ms or any command surfaces at Information.
            if (sw.ElapsedMilliseconds > 500 || requestType.EndsWith("Command", StringComparison.Ordinal))
            {
                _logger.LogInformation(
                    "{RequestType} completed in {ElapsedMs}ms",
                    requestType,
                    sw.ElapsedMilliseconds);
            }
            else
            {
                _logger.LogDebug(
                    "{RequestType} completed in {ElapsedMs}ms",
                    requestType,
                    sw.ElapsedMilliseconds);
            }

            return response;
        }
        catch (Exception ex)
        {
            sw.Stop();
            // Use Information (not Warning/Error) so handled domain exceptions don't
            // pollute error dashboards — the global exception middleware already
            // logs Warning/Error based on status mapping. We just record the timing
            // and exception type at Info level.
            _logger.LogInformation(
                "{RequestType} failed in {ElapsedMs}ms with {ExceptionType}",
                requestType,
                sw.ElapsedMilliseconds,
                ex.GetType().Name);
            throw;
        }
    }
}
