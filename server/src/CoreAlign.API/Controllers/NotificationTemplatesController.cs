using Asp.Versioning;
using CoreAlign.Application.Notifications.Repositories;
using CoreAlign.Domain.Entities.Notifications;
using CoreAlign.Domain.Enums;
using CoreAlign.Domain.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CoreAlign.API.Controllers;

[ApiController]
[Authorize(Roles = "TenantAdmin")]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/admin/notification-templates")]
public class NotificationTemplatesController : ControllerBase
{
    private readonly INotificationTemplateRepository _templates;
    private readonly ITenantContext _tenantContext;
    private readonly IUnitOfWork _uow;

    public NotificationTemplatesController(
        INotificationTemplateRepository templates,
        ITenantContext tenantContext,
        IUnitOfWork uow)
    {
        _templates = templates;
        _tenantContext = tenantContext;
        _uow = uow;
    }

    public sealed record UpsertNotificationTemplateRequest(
        string Key,
        NotificationChannel Channel,
        string Locale,
        string? Subject,
        string BodyTemplate);

    [HttpGet]
    public async Task<IActionResult> ListAsync(CancellationToken ct)
    {
        var tenantId = _tenantContext.RequireTenantId();
        var globalTemplates = await _templates.ListAsync(null, ct);
        var tenantTemplates = await _templates.ListAsync(tenantId, ct);
        return Ok(new
        {
            global = globalTemplates.Select(t => new { t.Key, t.Channel, t.Locale, t.Subject, t.BodyTemplate, t.IsActive }),
            tenant = tenantTemplates.Select(t => new { t.Key, t.Channel, t.Locale, t.Subject, t.BodyTemplate, t.IsActive })
        });
    }

    [HttpPut]
    public async Task<IActionResult> UpsertAsync([FromBody] UpsertNotificationTemplateRequest req, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(req);
        var tenantId = _tenantContext.RequireTenantId();
        var existing = await _templates.GetByKeyLocaleAsync(tenantId, req.Key, req.Channel, req.Locale, ct);
        if (existing is null)
        {
            var template = new NotificationTemplate(tenantId, req.Key, req.Channel, req.Locale, req.Subject, req.BodyTemplate);
            await _templates.AddAsync(template, ct);
        }
        else
        {
            existing.Update(req.Subject, req.BodyTemplate);
        }
        await _uow.SaveChangesAsync(ct);
        return NoContent();
    }
}
