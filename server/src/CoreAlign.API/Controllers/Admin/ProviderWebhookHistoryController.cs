using Asp.Versioning;
using CoreAlign.API.Common;
using CoreAlign.Application.Authorization;
using CoreAlign.Application.Common;
using CoreAlign.Application.Common.Outbox;
using CoreAlign.Application.Providers.Admin;
using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CoreAlign.API.Controllers.Admin;

[ApiController]
[ApiVersion("1.0")]
[Authorize(Policy = AdminPolicies.ProviderConfig)]
[Route("api/v{version:apiVersion}/admin/webhooks")]
public class ProviderWebhookHistoryController : ControllerBase
{
    private const int MaxPageSize = 200;

    private readonly IProviderWebhookInboxRepository _inboxRepository;
    private readonly IOutboxRepository _outboxRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IOutboxProcessor _outboxProcessor;
    private readonly ITenantContext _tenantContext;
    private readonly ILogger<ProviderWebhookHistoryController> _logger;

    public ProviderWebhookHistoryController(
        IProviderWebhookInboxRepository inboxRepository,
        IOutboxRepository outboxRepository,
        IUnitOfWork unitOfWork,
        IOutboxProcessor outboxProcessor,
        ITenantContext tenantContext,
        ILogger<ProviderWebhookHistoryController> logger)
    {
        _inboxRepository = inboxRepository;
        _outboxRepository = outboxRepository;
        _unitOfWork = unitOfWork;
        _outboxProcessor = outboxProcessor;
        _tenantContext = tenantContext;
        _logger = logger;
    }

    [HttpGet("inbox")]
    public async Task<IActionResult> List(
        [FromQuery] string? providerName,
        [FromQuery] DateTime? fromUtc,
        [FromQuery] DateTime? toUtc,
        [FromQuery] string? status,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        var tenantId = _tenantContext.RequireTenantId();
        var clampedPage = Math.Max(1, page);
        var clampedPageSize = Math.Clamp(pageSize, 1, MaxPageSize);

        var (rows, total) = await _inboxRepository.ListAsync(
            tenantId,
            providerName,
            fromUtc,
            toUtc,
            status,
            clampedPage,
            clampedPageSize,
            cancellationToken);

        var items = rows.Select(MapItem).ToArray();
        var result = new PagedResult<ProviderWebhookHistoryItemDto>
        {
            Items = items,
            Page = clampedPage,
            PageSize = clampedPageSize,
            Total = total,
        };
        return Ok(ApiResponse<PagedResult<ProviderWebhookHistoryItemDto>>.Success(result));
    }

    [HttpPost("inbox/{id:guid}/replay")]
    public async Task<IActionResult> Replay(Guid id, CancellationToken cancellationToken)
    {
        var tenantId = _tenantContext.RequireTenantId();
        var entry = await _inboxRepository.GetByIdAsync(id, cancellationToken);
        if (entry is null)
        {
            return NotFound(ApiResponse<object>.Failure($"Webhook entry {id} not found.", 404));
        }

        _tenantContext.EnsureSameTenant(entry.TenantId);

        var replayType = entry.Category switch
        {
            CoreAlign.Domain.Enums.ProviderCategory.EFatura => "provider.efatura.webhook.replay",
            CoreAlign.Domain.Enums.ProviderCategory.Payment => "provider.payment.webhook.replay",
            _ => "provider.webhook.replay",
        };

        var payloadEnvelope = System.Text.Json.JsonSerializer.Serialize(new
        {
            inboxId = entry.Id,
            providerName = entry.ProviderName,
            category = entry.Category.ToString(),
            eventType = entry.EventType,
            rawPayload = entry.PayloadJson,
            replayedAtUtc = DateTime.UtcNow,
        });

        var outbox = new OutboxMessage(replayType, payloadEnvelope)
        {
            TenantId = tenantId,
        };

        await _outboxRepository.AddAsync(outbox, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Webhook {InboxId} queued for replay (tenant {TenantId}, provider {Provider}).",
            entry.Id, tenantId, entry.ProviderName);

        try
        {
            await _outboxProcessor.DrainAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Outbox drain after webhook replay threw; replay row remains queued.");
        }

        return Ok(ApiResponse<object>.Success(new { outboxId = outbox.Id, inboxId = entry.Id, queued = true }));
    }

    private static ProviderWebhookHistoryItemDto MapItem(ProviderWebhookInbox row)
    {
        var status = !string.IsNullOrEmpty(row.ProcessingError)
            ? "Failed"
            : row.ProcessedAtUtc.HasValue ? "Processed" : "Pending";

        return new ProviderWebhookHistoryItemDto(
            row.Id,
            row.ProviderName,
            row.Category,
            row.EventType,
            status,
            row.ProcessingError,
            row.RetryCount,
            row.ReceivedAtUtc,
            row.ProcessedAtUtc);
    }
}
