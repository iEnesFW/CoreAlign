using Asp.Versioning;
using CoreAlign.API.Common;
using CoreAlign.Application.Manufacturing.Commands;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CoreAlign.API.Controllers;

[ApiController]
[Authorize]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/planned-production-orders")]
public class PlannedProductionOrdersController : ControllerBase
{
    private readonly IMediator _mediator;
    public PlannedProductionOrdersController(IMediator mediator) => _mediator = mediator;

    [HttpPost("{id:guid}/convert")]
    [Authorize(Roles = "TenantAdmin")]
    public async Task<IActionResult> ConvertToJob(Guid id, [FromBody] ConvertPlannedOrderToJobCommand command, CancellationToken ct)
        => (await _mediator.Send(command with { PlannedOrderId = id }, ct)).ToCreated();
}
