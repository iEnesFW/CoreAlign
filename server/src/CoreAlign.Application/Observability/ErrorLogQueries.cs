using CoreAlign.Domain.Entities.Observability;
using CoreAlign.Domain.Enums;
using CoreAlign.Domain.Interfaces;
using MediatR;

namespace CoreAlign.Application.Observability;

public sealed record ErrorLogListItemDto(
    Guid Id,
    string CorrelationId,
    DateTime OccurredAtUtc,
    string Source,
    string Severity,
    int? StatusCode,
    string? HttpMethod,
    string? Path,
    string? ClientPage,
    string? ExceptionType,
    string Message,
    Guid? TenantId,
    Guid? UserId,
    string? UserName,
    bool IsResolved);

public sealed record ErrorLogDetailDto(
    Guid Id,
    string CorrelationId,
    string? TraceId,
    DateTime OccurredAtUtc,
    string Source,
    string Severity,
    int? StatusCode,
    string? HttpMethod,
    string? Path,
    string? ExceptionType,
    string Message,
    string? StackTrace,
    Guid? TenantId,
    Guid? UserId,
    string? UserName,
    string? ClientPage,
    string? ClientComponent,
    string? UserAgent,
    string? ContextJson,
    bool IsResolved,
    string? ResolutionNotes,
    DateTime? ResolvedAtUtc);

public sealed record ErrorLogPageDto(IReadOnlyList<ErrorLogListItemDto> Items, int Total, int Page, int PageSize);

public sealed record GetErrorLogsQuery(
    Guid? TenantIdFilter,
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
    int Page = 1,
    int PageSize = 25) : IRequest<ErrorLogPageDto>;

public sealed record GetErrorLogByIdQuery(Guid Id) : IRequest<ErrorLogDetailDto?>;

public sealed class GetErrorLogsHandler : IRequestHandler<GetErrorLogsQuery, ErrorLogPageDto>
{
    private readonly IErrorLogRepository _repository;
    public GetErrorLogsHandler(IErrorLogRepository repository) => _repository = repository;

    public async Task<ErrorLogPageDto> Handle(GetErrorLogsQuery q, CancellationToken ct)
    {
        var page = Math.Max(1, q.Page);
        var pageSize = Math.Clamp(q.PageSize, 1, 200);
        var query = new ErrorLogQuery(
            q.TenantIdFilter, q.Severity, q.Source, q.StatusCode, q.CorrelationId,
            q.PathContains, q.UserId, q.OnlyUnresolved, q.FromUtc, q.ToUtc, q.Search,
            (page - 1) * pageSize, pageSize);

        var (items, total) = await _repository.QueryAsync(query, ct);
        return new ErrorLogPageDto(items.Select(Map).ToList(), total, page, pageSize);
    }

    private static ErrorLogListItemDto Map(ErrorLogEntry e) => new(
        e.Id, e.CorrelationId, e.OccurredAtUtc, e.Source.ToString(), e.Severity.ToString(),
        e.StatusCode, e.HttpMethod, e.Path, e.ClientPage, e.ExceptionType, e.Message,
        e.TenantId, e.UserId, e.UserName, e.IsResolved);
}

public sealed class GetErrorLogByIdHandler : IRequestHandler<GetErrorLogByIdQuery, ErrorLogDetailDto?>
{
    private readonly IErrorLogRepository _repository;
    public GetErrorLogByIdHandler(IErrorLogRepository repository) => _repository = repository;

    public async Task<ErrorLogDetailDto?> Handle(GetErrorLogByIdQuery q, CancellationToken ct)
    {
        var e = await _repository.GetByIdAsync(q.Id, ct);
        if (e is null) return null;
        return new ErrorLogDetailDto(
            e.Id, e.CorrelationId, e.TraceId, e.OccurredAtUtc, e.Source.ToString(), e.Severity.ToString(),
            e.StatusCode, e.HttpMethod, e.Path, e.ExceptionType, e.Message, e.StackTrace,
            e.TenantId, e.UserId, e.UserName, e.ClientPage, e.ClientComponent, e.UserAgent, e.ContextJson,
            e.IsResolved, e.ResolutionNotes, e.ResolvedAtUtc);
    }
}
