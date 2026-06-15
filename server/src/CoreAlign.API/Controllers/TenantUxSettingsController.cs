using Asp.Versioning;
using CoreAlign.Application.Identity.PersonaPreference.Commands;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CoreAlign.API.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Authorize(Roles = "TenantAdmin")]
[Route("api/v{version:apiVersion}/admin/tenant-settings/ux")]
public class TenantUxSettingsController : ControllerBase
{
    private readonly IMediator _mediator;

    public TenantUxSettingsController(IMediator mediator) => _mediator = mediator;

    [HttpPut("default-mode")]
    public async Task<IActionResult> SetDefault([FromBody] SetTenantUxDefaultCommand cmd, CancellationToken ct)
    {
        await _mediator.Send(cmd, ct);
        return NoContent();
    }
}
