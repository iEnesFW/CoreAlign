using Asp.Versioning;
using CoreAlign.API.Authorization;
using CoreAlign.API.Common;
using CoreAlign.Application.Common;
using CoreAlign.Application.Platform.Tenants;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CoreAlign.API.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Authorize(Roles = PersonaPolicies.PlatformAdminRole)]
[Route("api/v{version:apiVersion}/platform/tenants")]
public class TenantsController : ControllerBase
{
    private readonly IMediator _mediator;
    public TenantsController(IMediator mediator) => _mediator = mediator;

    [HttpGet]
    public async Task<IActionResult> List([FromQuery] string? search, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, [FromQuery] bool includeArchived = false, CancellationToken ct = default)
        => (await _mediator.Send(new ListPlatformTenantsQuery(search, page, pageSize, includeArchived), ct)).ToOk();

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var dto = await _mediator.Send(new GetPlatformTenantQuery(id), ct);
        return dto is null
            ? NotFound(ApiResponse<object>.Failure("Tenant not found.", 404))
            : dto.ToOk();
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdatePlatformTenantCommand cmd, CancellationToken ct)
    {
        if (id != cmd.Id)
        {
            return BadRequest(ApiResponse<object>.Failure("Route id does not match command id.", 400));
        }
        return (await _mediator.Send(cmd, ct)).ToOk();
    }

    [HttpPost("{id:guid}/archive")]
    public async Task<IActionResult> Archive(Guid id, CancellationToken ct)
        => (await _mediator.Send(new ArchivePlatformTenantCommand(id), ct)).ToOk();

    [HttpPost("{id:guid}/restore")]
    public async Task<IActionResult> Restore(Guid id, CancellationToken ct)
        => (await _mediator.Send(new RestorePlatformTenantCommand(id), ct)).ToOk();
}
