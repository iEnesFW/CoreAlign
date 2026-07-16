using Asp.Versioning;
using CoreAlign.API.Common;
using CoreAlign.Application.Manufacturing.Commands;
using CoreAlign.Application.Manufacturing.Queries;
using CoreAlign.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CoreAlign.API.Controllers;

[ApiController]
[Authorize]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/production-routings")]
public class ProductionRoutingsController : ControllerBase
{
    private readonly IMediator _mediator;
    public ProductionRoutingsController(IMediator mediator) => _mediator = mediator;

    [HttpGet]
    public async Task<IActionResult> List([FromQuery] RoutingStatus? status, [FromQuery] int take, CancellationToken ct)
        => (await _mediator.Send(new ListProductionRoutingsQuery(status, take <= 0 ? 100 : take), ct)).ToOk();

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
        => (await _mediator.Send(new GetProductionRoutingByIdQuery(id), ct)).ToOk();

    [HttpPost]
    [Authorize(Roles = "TenantAdmin")]
    public async Task<IActionResult> Create([FromBody] CreateProductionRoutingCommand command, CancellationToken ct)
        => (await _mediator.Send(command, ct)).ToCreated();

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "TenantAdmin")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateProductionRoutingCommand command, CancellationToken ct)
        => (await _mediator.Send(command with { Id = id }, ct)).ToOk();

    [HttpPut("{id:guid}/steps")]
    [Authorize(Roles = "TenantAdmin")]
    public async Task<IActionResult> SetSteps(Guid id, [FromBody] SetRoutingStepsCommand command, CancellationToken ct)
        => (await _mediator.Send(command with { RoutingId = id }, ct)).ToOk();

    [HttpPost("{id:guid}/activate")]
    [Authorize(Roles = "TenantAdmin")]
    public async Task<IActionResult> Activate(Guid id, CancellationToken ct)
        => (await _mediator.Send(new ActivateRoutingCommand(id), ct)).ToOk();

    [HttpPost("{id:guid}/archive")]
    [Authorize(Roles = "TenantAdmin")]
    public async Task<IActionResult> Archive(Guid id, CancellationToken ct)
        => (await _mediator.Send(new ArchiveRoutingCommand(id), ct)).ToOk();

    [HttpPost("{id:guid}/restore")]
    [Authorize(Roles = "TenantAdmin")]
    public async Task<IActionResult> Restore(Guid id, CancellationToken ct)
        => (await _mediator.Send(new RestoreRoutingToDraftCommand(id), ct)).ToOk();

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "TenantAdmin")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
        => (await _mediator.Send(new DeleteProductionRoutingCommand(id), ct)).ToOk();

    [HttpPost("assign-product")]
    [Authorize(Roles = "TenantAdmin")]
    public async Task<IActionResult> AssignToProduct([FromBody] AssignRoutingToProductCommand command, CancellationToken ct)
        => (await _mediator.Send(command, ct)).ToOk();
}
