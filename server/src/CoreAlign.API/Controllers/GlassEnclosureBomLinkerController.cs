using Asp.Versioning;
using CoreAlign.API.Common;
using CoreAlign.Application.GlassEnclosure.Bom;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CoreAlign.API.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Authorize(Roles = "TenantAdmin")]
[Route("api/v{version:apiVersion}/admin/glass-enclosure/bom-linker")]
public class GlassEnclosureBomLinkerController : ControllerBase
{
    private readonly IMediator _mediator;

    public GlassEnclosureBomLinkerController(IMediator mediator) => _mediator = mediator;

    [HttpPost("backfill")]
    public async Task<IActionResult> Backfill(CancellationToken ct) =>
        (await _mediator.Send(new BomLineBackfillCommand(), ct)).ToOk();
}
