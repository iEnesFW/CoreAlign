using CoreAlign.Domain.Entities.Observability;
using CoreAlign.Domain.Enums;

namespace CoreAlign.Domain.Interfaces;

public sealed record ErrorLogQuery(
    Guid? TenantId,
    ErrorSeverity? Severity,
    ErrorSource? Source,
    int? StatusCode,
    string? CorrelationId,
    string? PathContains,
    Guid? UserId,
    bool? OnlyUnresolved,
    DateTime? FromUtc,
    DateTime? ToUtc,
    string? Search,
    int Skip,
    int Take);

public interface IErrorLogRepository
{
    Task AddAsync(ErrorLogEntry entry, CancellationToken cancellationToken = default);
    Task<ErrorLogEntry?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<(IReadOnlyList<ErrorLogEntry> Items, int Total)> QueryAsync(ErrorLogQuery query, CancellationToken cancellationToken = default);
    Task<int> DeleteOlderThanAsync(DateTime thresholdUtc, CancellationToken cancellationToken = default);
}
