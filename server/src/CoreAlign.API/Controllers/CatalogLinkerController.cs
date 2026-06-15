using Asp.Versioning;
using CoreAlign.API.Common;
using CoreAlign.Application.Catalog.Linker;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CoreAlign.API.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Authorize(Roles = "TenantAdmin")]
[Route("api/v{version:apiVersion}/admin/catalog-linker")]
public class CatalogLinkerController : ControllerBase
{
    private readonly IMediator _mediator;

    public CatalogLinkerController(IMediator mediator) => _mediator = mediator;

    [HttpPost("dry-run")]
    public async Task<IActionResult> DryRun(CancellationToken ct) =>
        (await _mediator.Send(new CatalogLinkageDryRunCommand(), ct)).ToOk();

    [HttpPost("backfill")]
    public async Task<IActionResult> Backfill(CancellationToken ct) =>
        (await _mediator.Send(new CatalogLinkageBackfillCommand(), ct)).ToOk();
}
