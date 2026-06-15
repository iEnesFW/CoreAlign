using System.Security.Claims;
using Asp.Versioning;
using CoreAlign.Application.Notifications.Repositories;
using CoreAlign.Domain.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CoreAlign.API.Controllers;

[ApiController]
[Authorize]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/notification-messages")]
public class MyNotificationMessagesController : ControllerBase
{
    private readonly INotificationMessageRepository _messages;
    private readonly ITenantContext _tenantContext;

    public MyNotificationMessagesController(INotificationMessageRepository messages, ITenantContext tenantContext)
    {
        _messages = messages;
        _tenantContext = tenantContext;
    }

    private Guid CurrentUserId() => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet("me")]
    public async Task<IActionResult> ListMineAsync(
        [FromQuery] bool unreadOnly = false,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25,
        CancellationToken ct = default)
    {
        var tenantId = _tenantContext.RequireTenantId();
        var userId = CurrentUserId();
        var skip = Math.Max(0, (page - 1) * pageSize);
        var items = await _messages.ListForUserAsync(tenantId, userId, unreadOnly, skip, pageSize, ct);
        return Ok(items.Select(m => new
        {
            m.Id,
            m.Channel,
            m.Status,
            m.TemplateKey,
            m.CategoryKey,
            m.Subject,
            m.BodyMarkdown,
            m.CreatedAtUtc,
            m.ReadAtUtc
        }));
    }

    [HttpGet("me/unread-count")]
    public async Task<IActionResult> UnreadCountAsync(CancellationToken ct)
    {
        var tenantId = _tenantContext.RequireTenantId();
        var userId = CurrentUserId();
        var count = await _messages.CountUnreadAsync(tenantId, userId, ct);
        return Ok(new { unread = count });
    }

    [HttpPost("{id:guid}/mark-read")]
    public async Task<IActionResult> MarkReadAsync(Guid id, CancellationToken ct)
    {
        var tenantId = _tenantContext.RequireTenantId();
        var entity = await _messages.GetByIdAsync(tenantId, id, ct);
        if (entity is null) return NotFound();
        if (entity.UserId != CurrentUserId()) return Forbid();
        entity.MarkRead(DateTime.UtcNow);
        return NoContent();
    }
}
