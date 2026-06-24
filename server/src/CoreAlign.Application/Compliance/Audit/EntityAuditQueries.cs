using CoreAlign.Application.Common.Audit;
using CoreAlign.Domain.Entities.Compliance;
using MediatR;

namespace CoreAlign.Application.Compliance.Audit;

public sealed record EntityAuditLogDto(
    Guid Id,
    string EntityType,
    Guid EntityId,
    string Action,
    string? BeforeJson,
    string? AfterJson,
    Guid? UserId,
    DateTime ChangedAtUtc,
    Guid? CorrelationId,
    string RollingHash,
    long Sequence);

public sealed record GetEntityAuditTimelineQuery(string EntityType, Guid EntityId)
    : IRequest<IReadOnlyList<EntityAuditLogDto>>;

public interface IEntityAuditLogRepository
{
    Task<IReadOnlyList<EntityAuditLog>> GetTimelineAsync(string entityType, Guid entityId, CancellationToken ct);

    Task<(IReadOnlyList<EntityAuditLog> Items, int Total)> SearchAsync(
        Guid tenantId,
        IReadOnlyList<string>? entityTypes,
        DateTime? fromUtc,
        DateTime? toUtc,
        int page,
        int pageSize,
        CancellationToken ct);

    Task<(IReadOnlyList<EntityAuditLog> Items, int Total)> SearchAdvancedAsync(
        Guid tenantId,
        AuditLogSearchCriteria criteria,
        int page,
        int pageSize,
        CancellationToken ct);

    IAsyncEnumerable<EntityAuditLog> StreamAsync(
        Guid tenantId,
        AuditLogSearchCriteria criteria,
        int batchSize,
        CancellationToken ct);
}

public sealed record AuditLogSearchCriteria(
    DateTime? FromUtc,
    DateTime? ToUtc,
    IReadOnlyList<string>? EntityTypes,
    IReadOnlyList<EntityAuditAction>? Actions,
    Guid? UserId,
    Guid? EntityId);

public static class EntityAuditLogMapper
{
    public static EntityAuditLogDto ToDto(EntityAuditLog log, IAuditFieldRedactor redactor) => new(
        log.Id,
        log.EntityType,
        log.EntityId,
        log.Action.ToString(),
        redactor.RedactJson(log.BeforeJson),
        redactor.RedactJson(log.AfterJson),
        log.UserId,
        log.ChangedAtUtc,
        log.CorrelationId,
        log.RollingHash,
        log.Sequence);
}

public sealed class GetEntityAuditTimelineHandler : IRequestHandler<GetEntityAuditTimelineQuery, IReadOnlyList<EntityAuditLogDto>>
{
    private readonly IEntityAuditLogRepository _repo;
    private readonly IAuditFieldRedactor _redactor;
    public GetEntityAuditTimelineHandler(IEntityAuditLogRepository repo, IAuditFieldRedactor redactor)
    {
        _repo = repo;
        _redactor = redactor;
    }
    public async Task<IReadOnlyList<EntityAuditLogDto>> Handle(GetEntityAuditTimelineQuery query, CancellationToken cancellationToken)
    {
        var items = await _repo.GetTimelineAsync(query.EntityType, query.EntityId, cancellationToken);
        return items.Select(log => EntityAuditLogMapper.ToDto(log, _redactor)).ToArray();
    }
}
