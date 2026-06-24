using CoreAlign.Application.Common.Observability;
using CoreAlign.Domain.Entities.Observability;
using CoreAlign.Domain.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace CoreAlign.Infrastructure.Observability;

public sealed class ErrorLogWriter : IErrorLogWriter
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ErrorLogWriter> _logger;

    public ErrorLogWriter(IServiceScopeFactory scopeFactory, ILogger<ErrorLogWriter> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public async Task WriteAsync(ErrorLogRecord record, CancellationToken cancellationToken = default)
    {
        try
        {
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeout.CancelAfter(TimeSpan.FromSeconds(5));
            cancellationToken = timeout.Token;

            using var scope = _scopeFactory.CreateScope();
            var repository = scope.ServiceProvider.GetRequiredService<IErrorLogRepository>();
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

            var entry = new ErrorLogEntry(
                TruncateRequired(record.CorrelationId, 64),
                record.Source,
                record.Severity,
                TruncateRequired(record.Message, 8000),
                DateTime.UtcNow,
                Truncate(record.TraceId, 128),
                record.StatusCode,
                Truncate(record.HttpMethod, 8),
                Truncate(record.Path, 512),
                Truncate(record.ExceptionType, 256),
                Truncate(record.StackTrace, 16000),
                record.TenantId,
                record.UserId,
                Truncate(record.UserName, 256),
                Truncate(record.ClientPage, 512),
                Truncate(record.ClientComponent, 256),
                Truncate(record.UserAgent, 512),
                Truncate(record.ContextJson, 16000));

            await repository.AddAsync(entry, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            ErrorLogMetrics.RecordPersisted(record.Severity.ToString(), record.Source.ToString());
        }
        catch (Exception ex)
        {
            ErrorLogMetrics.RecordWriteFailure();
            _logger.LogError(ex, "Failed to persist error log entry for correlation {CorrelationId}.", record.CorrelationId);
        }
    }

    private static string? Truncate(string? value, int max)
    {
        if (string.IsNullOrEmpty(value)) return value;
        return value.Length <= max ? value : value[..max];
    }

    private static string TruncateRequired(string value, int max) =>
        value.Length <= max ? value : value[..max];
}
