using Asp.Versioning;
using CoreAlign.API.Authorization;
using CoreAlign.API.Common;
using CoreAlign.Application.Mrp;
using CoreAlign.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CoreAlign.API.Controllers;

[ApiController]
[Authorize(Policy = PersonaPolicies.Tenant)]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/purchase-requisitions")]
public class PurchaseRequisitionsController : ControllerBase
{
    private readonly IMediator _mediator;
    public PurchaseRequisitionsController(IMediator mediator) => _mediator = mediator;

    [HttpGet]
    public async Task<IActionResult> Search(
        [FromQuery] PurchaseRequisitionStatus? status,
        [FromQuery] Guid? productId,
        [FromQuery] DateTime? fromUtc,
        [FromQuery] DateTime? toUtc,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25,
        CancellationToken ct = default)
        => (await _mediator.Send(new ListPurchaseRequisitionsQuery(status, productId, fromUtc, toUtc, page, pageSize), ct)).ToOk();

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreatePurchaseRequisitionCommand cmd, CancellationToken ct)
        => (await _mediator.Send(cmd, ct)).ToCreated();

    [HttpPost("{id:guid}/submit")]
    public async Task<IActionResult> Submit(Guid id, CancellationToken ct)
        => (await _mediator.Send(new SubmitPurchaseRequisitionCommand(id), ct)).ToOk();

    [HttpPost("{id:guid}/approve")]
    [Authorize(Roles = "TenantAdmin")]
    public async Task<IActionResult> Approve(Guid id, CancellationToken ct)
        => (await _mediator.Send(new ApprovePurchaseRequisitionCommand(id), ct)).ToOk();

    [HttpPost("{id:guid}/reject")]
    [Authorize(Roles = "TenantAdmin")]
    public async Task<IActionResult> Reject(Guid id, [FromBody] RejectPurchaseRequisitionCommand? cmd, CancellationToken ct)
        => (await _mediator.Send(new RejectPurchaseRequisitionCommand(id, cmd?.Reason), ct)).ToOk();

    [HttpPost("{id:guid}/cancel")]
    public async Task<IActionResult> Cancel(Guid id, [FromBody] CancelPurchaseRequisitionCommand? cmd, CancellationToken ct)
        => (await _mediator.Send(new CancelPurchaseRequisitionCommand(id, cmd?.Reason), ct)).ToOk();

    [HttpPost("{id:guid}/convert")]
    public async Task<IActionResult> Convert(Guid id, [FromBody] ConvertRequisitionToPurchaseOrderCommand cmd, CancellationToken ct)
        => id != cmd.Id
            ? new BadRequestObjectResult("Route id does not match command id.")
            : (await _mediator.Send(cmd, ct)).ToOk();
}
