using System.Security.Claims;
using Asp.Versioning;
using CoreAlign.Application.Notifications.Repositories;
using CoreAlign.Domain.Entities.Notifications;
using CoreAlign.Domain.Enums;
using CoreAlign.Domain.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CoreAlign.API.Controllers;

[ApiController]
[Authorize]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/users/me/notification-preferences")]
public class NotificationPreferencesController : ControllerBase
{
    private readonly INotificationPreferenceRepository _preferences;
    private readonly ITenantContext _tenantContext;
    private readonly IUnitOfWork _uow;

    public NotificationPreferencesController(
        INotificationPreferenceRepository preferences,
        ITenantContext tenantContext,
        IUnitOfWork uow)
    {
        _preferences = preferences;
        _tenantContext = tenantContext;
        _uow = uow;
    }

    private Guid CurrentUserId() => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    public sealed record UpsertNotificationPreferenceRequest(string CategoryKey, NotificationChannel Channel, bool IsEnabled);

    [HttpGet]
    public async Task<IActionResult> GetAsync(CancellationToken ct)
    {
        var tenantId = _tenantContext.RequireTenantId();
        var prefs = await _preferences.ListForUserAsync(tenantId, CurrentUserId(), ct);
        return Ok(prefs.Select(p => new { p.CategoryKey, p.Channel, p.IsEnabled }));
    }

    [HttpPatch]
    public async Task<IActionResult> PatchAsync([FromBody] UpsertNotificationPreferenceRequest req, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(req);
        var tenantId = _tenantContext.RequireTenantId();
        var userId = CurrentUserId();
        var existing = await _preferences.GetAsync(tenantId, userId, req.CategoryKey, req.Channel, ct);
        if (existing is null)
        {
            var pref = new NotificationPreference(tenantId, userId, req.CategoryKey, req.Channel, req.IsEnabled);
            await _preferences.AddAsync(pref, ct);
        }
        else
        {
            existing.Update(req.IsEnabled);
        }
        await _uow.SaveChangesAsync(ct);
        return NoContent();
    }
}
