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
[Route("api/v{version:apiVersion}/work-centers")]
public class WorkCentersController : ControllerBase
{
    private readonly IMediator _mediator;
    public WorkCentersController(IMediator mediator) => _mediator = mediator;

    [HttpGet]
    public async Task<IActionResult> List([FromQuery] bool includeInactive, CancellationToken ct)
        => (await _mediator.Send(new ListWorkCentersQuery(includeInactive), ct)).ToOk();

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
        => (await _mediator.Send(new GetWorkCenterByIdQuery(id), ct)).ToOk();

    [HttpPost]
    [Authorize(Roles = "TenantAdmin")]
    public async Task<IActionResult> Create([FromBody] CreateWorkCenterCommand command, CancellationToken ct)
        => (await _mediator.Send(command, ct)).ToCreated();

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "TenantAdmin")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateWorkCenterCommand command, CancellationToken ct)
        => (await _mediator.Send(command with { Id = id }, ct)).ToOk();
}
