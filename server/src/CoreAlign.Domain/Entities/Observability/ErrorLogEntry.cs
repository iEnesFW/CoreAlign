using CoreAlign.Domain.Common;
using CoreAlign.Domain.Enums;

namespace CoreAlign.Domain.Entities.Observability;

public class ErrorLogEntry : BaseEntity
{
    public string CorrelationId { get; private set; } = string.Empty;
    public string? TraceId { get; private set; }
    public DateTime OccurredAtUtc { get; private set; }
    public ErrorSource Source { get; private set; }
    public ErrorSeverity Severity { get; private set; }
    public int? StatusCode { get; private set; }
    public string? HttpMethod { get; private set; }
    public string? Path { get; private set; }
    public string? ExceptionType { get; private set; }
    public string Message { get; private set; } = string.Empty;
    public string? StackTrace { get; private set; }
    public Guid? TenantId { get; private set; }
    public Guid? UserId { get; private set; }
    public string? UserName { get; private set; }
    public string? ClientPage { get; private set; }
    public string? ClientComponent { get; private set; }
    public string? UserAgent { get; private set; }
    public string? ContextJson { get; private set; }
    public bool IsResolved { get; private set; }
    public string? ResolutionNotes { get; private set; }
    public DateTime? ResolvedAtUtc { get; private set; }
    public Guid? ResolvedByUserId { get; private set; }

    protected ErrorLogEntry() { }

    public ErrorLogEntry(
        string correlationId,
        ErrorSource source,
        ErrorSeverity severity,
        string message,
        DateTime occurredAtUtc,
        string? traceId = null,
        int? statusCode = null,
        string? httpMethod = null,
        string? path = null,
        string? exceptionType = null,
        string? stackTrace = null,
        Guid? tenantId = null,
        Guid? userId = null,
        string? userName = null,
        string? clientPage = null,
        string? clientComponent = null,
        string? userAgent = null,
        string? contextJson = null)
    {
        CorrelationId = string.IsNullOrWhiteSpace(correlationId) ? "unknown" : correlationId;
        Source = source;
        Severity = severity;
        Message = string.IsNullOrWhiteSpace(message) ? "(no message)" : message;
        OccurredAtUtc = occurredAtUtc;
        TraceId = traceId;
        StatusCode = statusCode;
        HttpMethod = httpMethod;
        Path = path;
        ExceptionType = exceptionType;
        StackTrace = stackTrace;
        TenantId = tenantId;
        UserId = userId;
        UserName = userName;
        ClientPage = clientPage;
        ClientComponent = clientComponent;
        UserAgent = userAgent;
        ContextJson = contextJson;
    }

    public void MarkResolved(Guid? resolvedByUserId, string? notes, DateTime utcNow)
    {
        IsResolved = true;
        ResolvedByUserId = resolvedByUserId;
        ResolutionNotes = notes;
        ResolvedAtUtc = utcNow;
        UpdatedAtUtc = utcNow;
    }
}
