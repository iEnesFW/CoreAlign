using Asp.Versioning;
using CoreAlign.Application.Notifications.Messages;
using CoreAlign.Application.Notifications.Repositories;
using CoreAlign.Domain.Enums;
using CoreAlign.Domain.Interfaces;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CoreAlign.API.Controllers;

[ApiController]
[Authorize(Roles = "TenantAdmin")]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/notification-messages")]
public class NotificationMessagesController : ControllerBase
{
    private readonly INotificationMessageRepository _messages;
    private readonly ITenantContext _tenantContext;
    private readonly IMediator _mediator;

    public NotificationMessagesController(INotificationMessageRepository messages, ITenantContext tenantContext, IMediator mediator)
    {
        _messages = messages;
        _tenantContext = tenantContext;
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<IActionResult> ListAsync(
        [FromQuery] NotificationStatus? status,
        [FromQuery] NotificationChannel? channel,
        [FromQuery] string? category,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25,
        CancellationToken ct = default)
    {
        var tenantId = _tenantContext.RequireTenantId();
        var skip = Math.Max(0, (page - 1) * pageSize);
        var items = await _messages.ListAsync(tenantId, status, category, channel, skip, pageSize, ct);
        return Ok(items.Select(m => new
        {
            m.Id,
            m.Channel,
            m.Status,
            m.TemplateKey,
            m.CategoryKey,
            m.Locale,
            m.RecipientAddress,
            m.Subject,
            m.SentAtUtc,
            m.DeliveredAtUtc,
            m.ReadAtUtc,
            m.RetryCount,
            m.ProviderUsed,
            m.FailureReason,
            m.CreatedAtUtc
        }));
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetAsync(Guid id, CancellationToken ct)
    {
        var tenantId = _tenantContext.RequireTenantId();
        var entity = await _messages.GetByIdAsync(tenantId, id, ct);
        if (entity is null) return NotFound();
        return Ok(entity);
    }

    [HttpPost("{id:guid}/resend")]
    public async Task<IActionResult> ResendAsync(Guid id, CancellationToken ct)
    {
        await _mediator.Send(new ResendNotificationMessageCommand(id), ct);
        return Accepted(new { id, queued = true });
    }
}
