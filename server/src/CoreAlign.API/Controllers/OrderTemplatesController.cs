using Asp.Versioning;
using CoreAlign.API.Authorization;
using CoreAlign.API.Common;
using CoreAlign.Application.Common;
using CoreAlign.Application.Sales.OrderTemplates.Commands;
using CoreAlign.Application.Sales.OrderTemplates.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CoreAlign.API.Controllers;

[ApiController]
[Authorize(Policy = PersonaPolicies.Tenant)]
[Authorize(Roles = PersonaPolicies.TenantAdminRole)]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/order-templates")]
public class OrderTemplatesController : ControllerBase
{
    private readonly IMediator _mediator;

    public OrderTemplatesController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<IActionResult> ListAsync(
        [FromQuery] Guid? customerId = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(new GetOrderTemplatesQuery(customerId, page, pageSize), cancellationToken);
        return result.ToOk();
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetAsync(Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetOrderTemplateByIdQuery(id), cancellationToken);
        if (result is null)
        {
            return NotFound(ApiResponse<object>.Failure("Order template not found.", 404));
        }
        return result.ToOk();
    }

    [HttpPost]
    public async Task<IActionResult> CreateAsync([FromBody] CreateOrderTemplateCommand command, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(command, cancellationToken);
        return result.ToCreated();
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> UpdateAsync(Guid id, [FromBody] UpdateOrderTemplateCommand command, CancellationToken cancellationToken)
    {
        if (id != command.Id)
        {
            return BadRequest(ApiResponse<object>.Failure("Route id does not match command id.", 400));
        }
        var result = await _mediator.Send(command, cancellationToken);
        return result.ToOk();
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new DeleteOrderTemplateCommand(id), cancellationToken);
        return result.ToOk();
    }

    [HttpPost("{id:guid}/activate")]
    public async Task<IActionResult> ActivateAsync(Guid id, CancellationToken cancellationToken)
        => (await _mediator.Send(new SetOrderTemplateActiveCommand(id, true), cancellationToken)).ToOk();

    [HttpPost("{id:guid}/deactivate")]
    public async Task<IActionResult> DeactivateAsync(Guid id, CancellationToken cancellationToken)
        => (await _mediator.Send(new SetOrderTemplateActiveCommand(id, false), cancellationToken)).ToOk();

    [HttpPost("{id:guid}/run")]
    public async Task<IActionResult> RunAsync(Guid id, CancellationToken cancellationToken)
    {
        var orderId = await _mediator.Send(new RunOrderTemplateNowCommand(id), cancellationToken);
        return new { OrderId = orderId }.ToOk();
    }
}
