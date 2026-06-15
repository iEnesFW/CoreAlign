using Asp.Versioning;
using CoreAlign.API.Common;
using CoreAlign.Application.Common;
using CoreAlign.Application.Pricing.Discounts.Commands;
using CoreAlign.Application.Pricing.Discounts.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CoreAlign.API.Controllers;

[ApiController]
[Authorize]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/discount-rules")]
public class DiscountRulesController : ControllerBase
{
    private readonly IMediator _mediator;
    public DiscountRulesController(IMediator mediator) => _mediator = mediator;

    private static IActionResult RouteMismatch() =>
        new BadRequestObjectResult(ApiResponse<object>.Failure("Route id does not match command id.", 400));

    [HttpGet]
    public async Task<IActionResult> List([FromQuery] bool? isActive, CancellationToken ct)
        => (await _mediator.Send(new ListDiscountRulesQuery(isActive), ct)).ToOk();

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
        => (await _mediator.Send(new GetDiscountRuleByIdQuery(id), ct)).ToOk();

    [HttpPost]
    [Authorize(Roles = "TenantAdmin")]
    public async Task<IActionResult> Create([FromBody] CreateDiscountRuleCommand cmd, CancellationToken ct)
        => (await _mediator.Send(cmd, ct)).ToCreated();

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "TenantAdmin")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateDiscountRuleCommand cmd, CancellationToken ct)
        => id != cmd.Id ? RouteMismatch() : (await _mediator.Send(cmd, ct)).ToOk();

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "TenantAdmin")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
        => (await _mediator.Send(new DeleteDiscountRuleCommand(id), ct)).ToOk();
}
