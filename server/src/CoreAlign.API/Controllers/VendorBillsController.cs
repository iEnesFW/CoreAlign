using Asp.Versioning;
using CoreAlign.API.Authorization;
using CoreAlign.API.Common;
using CoreAlign.Application.Common;
using CoreAlign.Application.Purchasing;
using CoreAlign.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CoreAlign.API.Controllers;

[ApiController]
[Authorize(Policy = PersonaPolicies.Tenant)]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/vendor-bills")]
public class VendorBillsController : ControllerBase
{
    private readonly IMediator _mediator;
    public VendorBillsController(IMediator mediator) => _mediator = mediator;

    private static IActionResult RouteIdMismatch() =>
        new BadRequestObjectResult(ApiResponse<object>.Failure("Route id does not match command id.", 400));

    [HttpGet]
    public async Task<IActionResult> Search(
        [FromQuery] Guid? vendorId,
        [FromQuery] VendorBillStatus? status,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25,
        CancellationToken ct = default)
        => (await _mediator.Send(new SearchVendorBillsQuery(vendorId, status, page, pageSize), ct)).ToOk();

    [HttpGet("aging")]
    public async Task<IActionResult> Aging([FromQuery] DateTime? asOf, CancellationToken ct)
        => (await _mediator.Send(new GetVendorAgingQuery(asOf), ct)).ToOk();

    [HttpGet("three-way-match")]
    public async Task<IActionResult> ThreeWayMatch(
        [FromQuery] Guid? vendorId,
        [FromQuery] DateTime? fromUtc,
        [FromQuery] DateTime? toUtc,
        CancellationToken ct = default)
        => (await _mediator.Send(new GetThreeWayMatchQuery(vendorId, fromUtc, toUtc), ct)).ToOk();

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
        => (await _mediator.Send(new GetVendorBillByIdQuery(id), ct)).ToOk();

    [HttpGet("{id:guid}/applications")]
    public async Task<IActionResult> GetApplications(Guid id, CancellationToken ct)
        => (await _mediator.Send(new GetVendorBillApplicationsQuery(id), ct)).ToOk();

    [HttpPost]
    [Authorize(Roles = "TenantAdmin")]
    public async Task<IActionResult> Create([FromBody] CreateVendorBillCommand cmd, CancellationToken ct)
        => (await _mediator.Send(cmd, ct)).ToCreated();

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "TenantAdmin")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateVendorBillCommand cmd, CancellationToken ct)
        => id != cmd.Id ? RouteIdMismatch() : (await _mediator.Send(cmd, ct)).ToOk();

    [HttpPost("{id:guid}/post")]
    [Authorize(Roles = "TenantAdmin")]
    public async Task<IActionResult> Post(Guid id, CancellationToken ct)
        => (await _mediator.Send(new PostVendorBillCommand(id), ct)).ToOk();

    [HttpPost("{id:guid}/approve")]
    [Authorize(Roles = "TenantAdmin")]
    public async Task<IActionResult> Approve(Guid id, CancellationToken ct)
        => (await _mediator.Send(new ApproveVendorBillCommand(id), ct)).ToOk();

    [HttpPost("{id:guid}/cancel")]
    [Authorize(Roles = "TenantAdmin")]
    public async Task<IActionResult> Cancel(Guid id, CancellationToken ct)
        => (await _mediator.Send(new CancelVendorBillCommand(id), ct)).ToOk();
}

[ApiController]
[Authorize(Policy = PersonaPolicies.Tenant)]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/vendor-payments")]
public class VendorPaymentsController : ControllerBase
{
    private readonly IMediator _mediator;
    public VendorPaymentsController(IMediator mediator) => _mediator = mediator;

    private static IActionResult RouteIdMismatch() =>
        new BadRequestObjectResult(ApiResponse<object>.Failure("Route id does not match command id.", 400));

    [HttpGet]
    public async Task<IActionResult> Search(
        [FromQuery] Guid? vendorId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25,
        CancellationToken ct = default)
        => (await _mediator.Send(new SearchVendorPaymentsQuery(vendorId, page, pageSize), ct)).ToOk();

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetVendorPaymentByIdQuery(id), ct);
        return result is null ? NotFound() : result.ToOk();
    }

    [HttpGet("{id:guid}/applications")]
    public async Task<IActionResult> GetApplications(Guid id, CancellationToken ct)
        => (await _mediator.Send(new GetVendorPaymentApplicationsQuery(id), ct)).ToOk();

    [HttpPost]
    [Authorize(Roles = "TenantAdmin")]
    public async Task<IActionResult> Create([FromBody] CreateVendorPaymentCommand cmd, CancellationToken ct)
        => (await _mediator.Send(cmd, ct)).ToCreated();

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "TenantAdmin")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateVendorPaymentCommand cmd, CancellationToken ct)
        => id != cmd.Id ? RouteIdMismatch() : (await _mediator.Send(cmd, ct)).ToOk();

    [HttpPost("{id:guid}/void")]
    [Authorize(Roles = "TenantAdmin")]
    public async Task<IActionResult> Void(Guid id, [FromBody] VoidVendorPaymentCommand cmd, CancellationToken ct)
        => id != cmd.Id ? RouteIdMismatch() : (await _mediator.Send(cmd, ct)).ToOk();

    [HttpPost("apply")]
    [Authorize(Roles = "TenantAdmin")]
    public async Task<IActionResult> Apply([FromBody] ApplyVendorPaymentCommand cmd, CancellationToken ct)
        => (await _mediator.Send(cmd, ct)).ToCreated();

    [HttpPost("offset-advance")]
    [Authorize(Roles = "TenantAdmin")]
    public async Task<IActionResult> OffsetAdvance([FromBody] OffsetVendorAdvanceCommand cmd, CancellationToken ct)
        => (await _mediator.Send(cmd, ct)).ToCreated();
}
