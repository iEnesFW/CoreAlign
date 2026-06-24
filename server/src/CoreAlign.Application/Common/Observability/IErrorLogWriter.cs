using CoreAlign.Domain.Enums;

namespace CoreAlign.Application.Common.Observability;

public sealed record ErrorLogRecord(
    string CorrelationId,
    ErrorSource Source,
    ErrorSeverity Severity,
    string Message,
    string? TraceId = null,
    int? StatusCode = null,
    string? HttpMethod = null,
    string? Path = null,
    string? ExceptionType = null,
    string? StackTrace = null,
    Guid? TenantId = null,
    Guid? UserId = null,
    string? UserName = null,
    string? ClientPage = null,
    string? ClientComponent = null,
    string? UserAgent = null,
    string? ContextJson = null);

public interface IErrorLogWriter
{
    Task WriteAsync(ErrorLogRecord record, CancellationToken cancellationToken = default);
}
