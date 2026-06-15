using Asp.Versioning;
using CoreAlign.API.Common;
using CoreAlign.Application.Authorization;
using CoreAlign.Application.Common;
using CoreAlign.Application.Compliance.Audit;
using CoreAlign.Application.Providers.Admin;
using CoreAlign.Domain.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CoreAlign.API.Controllers.Admin;

[ApiController]
[ApiVersion("1.0")]
[Authorize(Policy = AdminPolicies.ProviderConfig)]
[Route("api/v{version:apiVersion}/admin/audit")]
public class ProviderAuditController : ControllerBase
{
    private const int MaxPageSize = 200;

    private static readonly IReadOnlyList<string> ProviderEntityTypes = new[]
    {
        nameof(CoreAlign.Domain.Entities.TenantProviderConfig),
        nameof(CoreAlign.Domain.Entities.ProviderWebhookInbox),
        nameof(CoreAlign.Domain.Entities.OutboxMessage),
    };

    private readonly IEntityAuditLogRepository _auditRepository;
    private readonly ITenantContext _tenantContext;

    public ProviderAuditController(
        IEntityAuditLogRepository auditRepository,
        ITenantContext tenantContext)
    {
        _auditRepository = auditRepository;
        _tenantContext = tenantContext;
    }

    [HttpGet("provider-events")]
    public async Task<IActionResult> ListEvents(
        [FromQuery] DateTime? fromUtc,
        [FromQuery] DateTime? toUtc,
        [FromQuery] string? providerName,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        var tenantId = _tenantContext.RequireTenantId();
        var clampedPage = Math.Max(1, page);
        var clampedPageSize = Math.Clamp(pageSize, 1, MaxPageSize);

        var (rows, total) = await _auditRepository.SearchAsync(
            tenantId,
            ProviderEntityTypes,
            fromUtc,
            toUtc,
            clampedPage,
            clampedPageSize,
            cancellationToken);

        var items = rows
            .Where(r => string.IsNullOrWhiteSpace(providerName)
                        || ContainsProviderName(r.BeforeJson, providerName!)
                        || ContainsProviderName(r.AfterJson, providerName!))
            .Select(r => new ProviderAuditEventDto(
                r.Id,
                r.EntityType,
                r.EntityId,
                r.Action.ToString(),
                r.BeforeJson,
                r.AfterJson,
                r.UserId,
                r.ChangedAtUtc,
                r.CorrelationId,
                r.Sequence))
            .ToArray();

        var result = new PagedResult<ProviderAuditEventDto>
        {
            Items = items,
            Page = clampedPage,
            PageSize = clampedPageSize,
            Total = total,
        };
        return Ok(ApiResponse<PagedResult<ProviderAuditEventDto>>.Success(result));
    }

    private static bool ContainsProviderName(string? json, string providerName)
    {
        if (string.IsNullOrEmpty(json) || string.IsNullOrWhiteSpace(providerName))
        {
            return false;
        }

        return json.Contains(providerName, StringComparison.OrdinalIgnoreCase);
    }
}
