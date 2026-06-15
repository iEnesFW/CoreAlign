using Asp.Versioning;
using CoreAlign.API.Common;
using CoreAlign.Application.Common;
using CoreAlign.Application.Pricing.TaxRules.Commands;
using CoreAlign.Application.Pricing.TaxRules.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CoreAlign.API.Controllers;

[ApiController]
[Authorize]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/tax-rules")]
public class TaxRulesController : ControllerBase
{
    private readonly IMediator _mediator;
    public TaxRulesController(IMediator mediator) => _mediator = mediator;

    private static IActionResult RouteMismatch() =>
        new BadRequestObjectResult(ApiResponse<object>.Failure("Route id does not match command id.", 400));

    [HttpGet]
    public async Task<IActionResult> List([FromQuery] bool? isActive, CancellationToken ct)
        => (await _mediator.Send(new ListTaxRulesQuery(isActive), ct)).ToOk();

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
        => (await _mediator.Send(new GetTaxRuleByIdQuery(id), ct)).ToOk();

    [HttpPost]
    [Authorize(Roles = "TenantAdmin")]
    public async Task<IActionResult> Create([FromBody] CreateTaxRuleCommand cmd, CancellationToken ct)
        => (await _mediator.Send(cmd, ct)).ToCreated();

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "TenantAdmin")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateTaxRuleCommand cmd, CancellationToken ct)
        => id != cmd.Id ? RouteMismatch() : (await _mediator.Send(cmd, ct)).ToOk();

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "TenantAdmin")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
        => (await _mediator.Send(new DeleteTaxRuleCommand(id), ct)).ToOk();
}
