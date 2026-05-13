using Asp.Versioning;
using CoreAlign.API.Common;
using CoreAlign.Application.Accounting.Commands;
using CoreAlign.Application.Accounting.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CoreAlign.API.Controllers;

[ApiController]
[Authorize]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/accounting")]
public class AccountingController : ControllerBase
{
    private readonly IMediator _mediator;
    public AccountingController(IMediator mediator) => _mediator = mediator;

    [HttpGet("periods")]
    public async Task<IActionResult> ListPeriods([FromQuery] int? year, CancellationToken ct)
        => (await _mediator.Send(new ListAccountingPeriodsQuery(year), ct)).ToOk();

    [HttpGet("periods/{id:guid}")]
    public async Task<IActionResult> GetPeriod(Guid id, CancellationToken ct)
        => (await _mediator.Send(new GetAccountingPeriodByIdQuery(id), ct)).ToOk();

    [HttpPost("periods")]
    public async Task<IActionResult> CreatePeriod([FromBody] CreateAccountingPeriodCommand cmd, CancellationToken ct)
        => (await _mediator.Send(cmd, ct)).ToCreated();

    [HttpPost("periods/{id:guid}/close")]
    [Authorize(Roles = "TenantAdmin")]
    public async Task<IActionResult> ClosePeriod(Guid id, [FromBody] ClosePeriodCommand? cmd, CancellationToken ct)
        => (await _mediator.Send(new ClosePeriodCommand(id, cmd?.ClosedByUserId, cmd?.Notes), ct)).ToOk();

    [HttpPost("periods/{id:guid}/reopen")]
    [Authorize(Roles = "TenantAdmin")]
    public async Task<IActionResult> ReopenPeriod(Guid id, [FromBody] ReopenPeriodCommand? cmd, CancellationToken ct)
        => (await _mediator.Send(new ReopenPeriodCommand(id, cmd?.ReopenedByUserId), ct)).ToOk();

    [HttpPost("periods/{id:guid}/lock")]
    [Authorize(Roles = "TenantAdmin")]
    public async Task<IActionResult> LockPeriod(Guid id, [FromBody] LockPeriodCommand? cmd, CancellationToken ct)
        => (await _mediator.Send(new LockPeriodCommand(id, cmd?.LockedByUserId), ct)).ToOk();
}

[ApiController]
[Authorize]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/pricing")]
public class PricingController : ControllerBase
{
    private readonly IMediator _mediator;
    public PricingController(IMediator mediator) => _mediator = mediator;

    [HttpGet("resolve")]
    public async Task<IActionResult> Resolve(
        [FromQuery] Guid productId,
        [FromQuery] Guid customerId,
        [FromQuery] decimal quantity = 1m,
        [FromQuery] string? currency = null,
        CancellationToken ct = default)
        => (await _mediator.Send(new ResolvePriceQuery(productId, customerId, quantity, currency), ct)).ToOk();

    [HttpGet("customer-product-prices")]
    public async Task<IActionResult> GetCustomerProductPrices(
        [FromQuery] Guid? customerId,
        [FromQuery] Guid? productId,
        CancellationToken ct)
        => (await _mediator.Send(new GetCustomerProductPricesQuery(customerId, productId), ct)).ToOk();

    [HttpPost("customer-product-prices")]
    public async Task<IActionResult> Create([FromBody] CreateCustomerProductPriceCommand cmd, CancellationToken ct)
        => (await _mediator.Send(cmd, ct)).ToCreated();

    [HttpPut("customer-product-prices/{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateCustomerProductPriceCommand cmd, CancellationToken ct)
    {
        if (id != cmd.Id) return BadRequest(Application.Common.ApiResponse<object>.Failure("Route id mismatch.", 400));
        return (await _mediator.Send(cmd, ct)).ToOk();
    }

    [HttpDelete("customer-product-prices/{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
        => (await _mediator.Send(new DeleteCustomerProductPriceCommand(id), ct)).ToOk();
}
