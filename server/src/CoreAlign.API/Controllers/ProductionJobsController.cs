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
[Route("api/v{version:apiVersion}/production-jobs")]
public class ProductionJobsController : ControllerBase
{
    private readonly IMediator _mediator;
    public ProductionJobsController(IMediator mediator) => _mediator = mediator;

    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] ProductionJobStatus? status,
        [FromQuery] Guid? productId,
        [FromQuery] int take = 100,
        CancellationToken ct = default)
        => (await _mediator.Send(new ListProductionJobsQuery(status, productId, take), ct)).ToOk();

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
        => (await _mediator.Send(new GetProductionJobByIdQuery(id), ct)).ToOk();

    [HttpPost]
    [Authorize(Roles = "TenantAdmin")]
    public async Task<IActionResult> Create([FromBody] CreateProductionJobCommand command, CancellationToken ct)
        => (await _mediator.Send(command, ct)).ToCreated();

    [HttpPost("{id:guid}/release")]
    [Authorize(Roles = "TenantAdmin")]
    public async Task<IActionResult> Release(Guid id, [FromBody] ReleaseProductionJobCommand command, CancellationToken ct)
        => (await _mediator.Send(command with { Id = id }, ct)).ToOk();

    [HttpPost("{id:guid}/steps/{stepNumber:int}/start")]
    public async Task<IActionResult> StartStep(Guid id, int stepNumber, [FromBody] StartJobStepCommand command, CancellationToken ct)
        => (await _mediator.Send(command with { JobId = id, StepNumber = stepNumber }, ct)).ToOk();

    [HttpPost("{id:guid}/steps/{stepNumber:int}/finish")]
    public async Task<IActionResult> FinishStep(Guid id, int stepNumber, [FromBody] FinishJobStepCommand command, CancellationToken ct)
        => (await _mediator.Send(command with { JobId = id, StepNumber = stepNumber }, ct)).ToOk();

    [HttpPost("{id:guid}/steps/{stepNumber:int}/skip")]
    public async Task<IActionResult> SkipStep(Guid id, int stepNumber, [FromBody] SkipJobStepCommand command, CancellationToken ct)
        => (await _mediator.Send(command with { JobId = id, StepNumber = stepNumber }, ct)).ToOk();

    [HttpPost("{id:guid}/rework")]
    public async Task<IActionResult> Rework(Guid id, [FromBody] ReworkToStepCommand command, CancellationToken ct)
        => (await _mediator.Send(command with { JobId = id }, ct)).ToOk();

    [HttpPost("{id:guid}/hold")]
    [Authorize(Roles = "TenantAdmin")]
    public async Task<IActionResult> PutOnHold(Guid id, CancellationToken ct)
        => (await _mediator.Send(new PutJobOnHoldCommand(id), ct)).ToOk();

    [HttpPost("{id:guid}/resume")]
    [Authorize(Roles = "TenantAdmin")]
    public async Task<IActionResult> Resume(Guid id, CancellationToken ct)
        => (await _mediator.Send(new ResumeJobCommand(id), ct)).ToOk();

    [HttpPost("{id:guid}/cancel")]
    [Authorize(Roles = "TenantAdmin")]
    public async Task<IActionResult> Cancel(Guid id, [FromBody] CancelProductionJobCommand command, CancellationToken ct)
        => (await _mediator.Send(command with { Id = id }, ct)).ToOk();

    [HttpPost("{id:guid}/complete")]
    [Authorize(Roles = "TenantAdmin")]
    public async Task<IActionResult> Complete(Guid id, [FromBody] CompleteProductionJobCommand command, CancellationToken ct)
        => (await _mediator.Send(command with { Id = id }, ct)).ToOk();
}
