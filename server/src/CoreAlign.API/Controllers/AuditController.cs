using Asp.Versioning;
using CoreAlign.API.Authorization;
using CoreAlign.API.Common;
using CoreAlign.Application.Common;
using CoreAlign.Application.Compliance.Audit;
using CoreAlign.Domain.Interfaces;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CoreAlign.API.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Authorize(Roles = PersonaPolicies.TenantAdminRole)]
[Route("api/v{version:apiVersion}/audit")]
public class AuditController : ControllerBase
{
    private const int MaxPageSize = 200;
    private const int DefaultPageSize = 50;

    private readonly IMediator _mediator;
    private readonly IEntityAuditLogRepository _repository;
    private readonly IAuditLogExportService _exportService;
    private readonly ITenantContext _tenant;

    public AuditController(
        IMediator mediator,
        IEntityAuditLogRepository repository,
        IAuditLogExportService exportService,
        ITenantContext tenant)
    {
        _mediator = mediator;
        _repository = repository;
        _exportService = exportService;
        _tenant = tenant;
    }

    [HttpGet("entity/{entityType}/{entityId:guid}")]
    public async Task<IActionResult> GetTimeline(string entityType, Guid entityId, CancellationToken ct)
        => (await _mediator.Send(new GetEntityAuditTimelineQuery(entityType, entityId), ct)).ToOk();

    [HttpGet("search")]
    public async Task<IActionResult> Search(
        [FromQuery] DateTime? fromUtc,
        [FromQuery] DateTime? toUtc,
        [FromQuery] string? entityType,
        [FromQuery] string? action,
        [FromQuery] Guid? userId,
        [FromQuery] Guid? entityId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = DefaultPageSize,
        CancellationToken ct = default)
    {
        var tenantId = _tenant.RequireTenantId();
        var clampedPage = Math.Max(1, page);
        var clampedPageSize = Math.Clamp(pageSize, 1, MaxPageSize);
        var criteria = AuditSearchCriteriaBuilder.Build(fromUtc, toUtc, entityType, action, userId, entityId);
        var (rows, total) = await _repository.SearchAdvancedAsync(tenantId, criteria, clampedPage, clampedPageSize, ct);

        var items = rows.Select(EntityAuditLogMapper.ToDto).ToArray();
        var result = new PagedResult<EntityAuditLogDto>
        {
            Items = items,
            Page = clampedPage,
            PageSize = clampedPageSize,
            Total = total,
        };
        return result.ToOk();
    }

    [HttpGet("export")]
    public async Task<IActionResult> Export(
        [FromQuery] string format = "csv",
        [FromQuery] DateTime? fromUtc = null,
        [FromQuery] DateTime? toUtc = null,
        [FromQuery] string? entityType = null,
        [FromQuery] string? action = null,
        [FromQuery] Guid? userId = null,
        [FromQuery] Guid? entityId = null,
        CancellationToken ct = default)
    {
        if (!Enum.TryParse<AuditLogExportFormat>(format, ignoreCase: true, out var fmt))
        {
            return BadRequest(ApiResponse<string>.Failure($"Unsupported export format '{format}'."));
        }

        var entityTypes = SplitCsv(entityType);
        var actions = SplitCsv(action);
        var filter = new AuditLogExportFilter(fromUtc, toUtc, entityTypes, actions, userId, entityId);
        var result = await _exportService.ExportAsync(filter, fmt, ct);
        return File(result.Content, result.ContentType, result.FileName);
    }

    [HttpGet("schedule")]
    public async Task<IActionResult> GetSchedule(
        [FromServices] IScheduledAuditExportConfigRepository scheduleRepository,
        CancellationToken ct)
    {
        var tenantId = _tenant.RequireTenantId();
        var config = await scheduleRepository.GetForTenantAsync(tenantId, ct);
        return config.ToOk();
    }

    [HttpPut("schedule")]
    public async Task<IActionResult> UpsertSchedule(
        [FromBody] UpsertScheduledAuditExportRequest body,
        [FromServices] IScheduledAuditExportConfigRepository scheduleRepository,
        [FromServices] IUnitOfWork unitOfWork,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(body);
        var tenantId = _tenant.RequireTenantId();
        var existing = await scheduleRepository.GetForTenantAsync(tenantId, ct);
        var recipients = body.Recipients ?? Array.Empty<string>();
        var config = new ScheduledAuditExportConfig(
            body.Enabled,
            body.Frequency,
            body.Format,
            recipients.Where(r => !string.IsNullOrWhiteSpace(r)).Select(r => r.Trim()).ToArray(),
            Math.Clamp(body.LookbackDays, 1, 365),
            body.EntityTypes,
            existing?.LastRunAtUtc,
            existing?.LastRunStatus,
            existing?.LastRunError);
        await scheduleRepository.UpsertForTenantAsync(tenantId, config, ct);
        await unitOfWork.SaveChangesAsync(ct);
        return config.ToOk();
    }

    private static IReadOnlyList<string>? SplitCsv(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return null;
        var values = raw
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(v => !string.IsNullOrWhiteSpace(v))
            .ToArray();
        return values.Length > 0 ? values : null;
    }
}

public sealed record UpsertScheduledAuditExportRequest(
    bool Enabled,
    AuditExportFrequency Frequency,
    AuditLogExportFormat Format,
    int LookbackDays,
    IReadOnlyList<string>? Recipients,
    IReadOnlyList<string>? EntityTypes);

internal static class AuditSearchCriteriaBuilder
{
    public static AuditLogSearchCriteria Build(
        DateTime? fromUtc,
        DateTime? toUtc,
        string? entityType,
        string? action,
        Guid? userId,
        Guid? entityId)
    {
        IReadOnlyList<string>? entityTypes = null;
        if (!string.IsNullOrWhiteSpace(entityType))
        {
            entityTypes = entityType
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .ToArray();
        }

        IReadOnlyList<CoreAlign.Domain.Entities.Compliance.EntityAuditAction>? actions = null;
        if (!string.IsNullOrWhiteSpace(action))
        {
            var parsed = new List<CoreAlign.Domain.Entities.Compliance.EntityAuditAction>();
            foreach (var raw in action.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                if (Enum.TryParse<CoreAlign.Domain.Entities.Compliance.EntityAuditAction>(raw, ignoreCase: true, out var value))
                {
                    parsed.Add(value);
                }
            }
            if (parsed.Count > 0) actions = parsed;
        }

        return new AuditLogSearchCriteria(fromUtc, toUtc, entityTypes, actions, userId, entityId);
    }
}
