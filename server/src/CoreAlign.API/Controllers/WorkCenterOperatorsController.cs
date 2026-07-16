using Asp.Versioning;
using CoreAlign.API.Common;
using CoreAlign.Application.Manufacturing.Commands;
using CoreAlign.Application.Manufacturing.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CoreAlign.API.Controllers;

[ApiController]
[Authorize]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/work-center-operators")]
public class WorkCenterOperatorsController : ControllerBase
{
    private readonly IMediator _mediator;
    public WorkCenterOperatorsController(IMediator mediator) => _mediator = mediator;

    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] Guid? workCenterId,
        [FromQuery] Guid? employeeId,
        [FromQuery] int take,
        CancellationToken ct)
        => (await _mediator.Send(
            new ListWorkCenterOperatorsQuery(workCenterId, employeeId, take <= 0 ? 200 : take), ct)).ToOk();

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
        => (await _mediator.Send(new GetWorkCenterOperatorByIdQuery(id), ct)).ToOk();

    [HttpPost]
    [Authorize(Roles = "TenantAdmin")]
    public async Task<IActionResult> Create([FromBody] CreateWorkCenterOperatorCommand command, CancellationToken ct)
        => (await _mediator.Send(command, ct)).ToCreated();

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "TenantAdmin")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateWorkCenterOperatorCommand command, CancellationToken ct)
        => (await _mediator.Send(command with { Id = id }, ct)).ToOk();

    [HttpPost("{id:guid}/deactivate")]
    [Authorize(Roles = "TenantAdmin")]
    public async Task<IActionResult> Deactivate(Guid id, CancellationToken ct)
        => (await _mediator.Send(new DeactivateWorkCenterOperatorCommand(id), ct)).ToOk();

    [HttpPost("{id:guid}/activate")]
    [Authorize(Roles = "TenantAdmin")]
    public async Task<IActionResult> Activate(Guid id, CancellationToken ct)
        => (await _mediator.Send(new ActivateWorkCenterOperatorCommand(id), ct)).ToOk();
}
