using Asp.Versioning;
using CoreAlign.API.Authorization;
using CoreAlign.API.Common;
using CoreAlign.Application.Observability;
using CoreAlign.Domain.Enums;
using CoreAlign.Domain.Interfaces;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CoreAlign.API.Controllers.Admin;

[ApiController]
[Authorize(Roles = "PlatformAdmin,TenantAdmin")]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/admin/error-logs")]
public class ErrorLogsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ITenantContext _tenantContext;

    public ErrorLogsController(IMediator mediator, ITenantContext tenantContext)
    {
        _mediator = mediator;
        _tenantContext = tenantContext;
    }

    private bool IsPlatformAdmin => User.IsInRole(PersonaPolicies.PlatformAdminRole);

    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] ErrorSeverity? severity,
        [FromQuery] ErrorSource? source,
        [FromQuery] int? statusCode,
        [FromQuery] string? correlationId,
        [FromQuery] string? path,
        [FromQuery] Guid? userId,
        [FromQuery] bool? onlyUnresolved,
        [FromQuery] DateTime? fromUtc,
        [FromQuery] DateTime? toUtc,
        [FromQuery] string? search,
        [FromQuery] Guid? tenantId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25,
        CancellationToken ct = default)
    {
        var tenantFilter = IsPlatformAdmin ? tenantId : _tenantContext.RequireTenantId();
        var query = new GetErrorLogsQuery(
            tenantFilter, severity, source, statusCode, correlationId, path, userId,
            onlyUnresolved, fromUtc, toUtc, search, page, pageSize);
        return (await _mediator.Send(query, ct)).ToOk();
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id, CancellationToken ct)
    {
        var dto = await _mediator.Send(new GetErrorLogByIdQuery(id), ct);
        if (dto is null) return NotFound();
        if (!IsPlatformAdmin && dto.TenantId != _tenantContext.CurrentTenantId) return NotFound();
        return dto.ToOk();
    }

    [HttpPost("{id:guid}/resolve")]
    public async Task<IActionResult> Resolve(Guid id, [FromBody] ResolveErrorLogBody body, CancellationToken ct)
    {
        if (!IsPlatformAdmin)
        {
            var dto = await _mediator.Send(new GetErrorLogByIdQuery(id), ct);
            if (dto is null || dto.TenantId != _tenantContext.CurrentTenantId) return NotFound();
        }

        await _mediator.Send(new ResolveErrorLogCommand(id, body?.Notes), ct);
        return NoContent();
    }
}

public sealed record ResolveErrorLogBody(string? Notes);
