using Asp.Versioning;
using CoreAlign.API.Common;
using CoreAlign.Application.GlassPlates.Commands;
using CoreAlign.Application.GlassPlates.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CoreAlign.API.Controllers;

[ApiController]
[Authorize]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/storage-locations")]
public class StorageLocationsController : ControllerBase
{
    private readonly IMediator _mediator;
    public StorageLocationsController(IMediator mediator) => _mediator = mediator;

    [HttpPost]
    [Authorize(Roles = "TenantAdmin")]
    public async Task<IActionResult> Create([FromBody] CreateStorageLocationCommand command, CancellationToken ct)
        => (await _mediator.Send(command, ct)).ToOk();

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "TenantAdmin")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateStorageLocationCommand command, CancellationToken ct)
        => (await _mediator.Send(command with { Id = id }, ct)).ToOk();

    [HttpGet]
    public async Task<IActionResult> List([FromQuery] Guid? warehouseId, CancellationToken ct)
        => (await _mediator.Send(new ListStorageLocationsQuery(warehouseId), ct)).ToOk();
}
