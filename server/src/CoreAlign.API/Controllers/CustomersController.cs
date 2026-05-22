using Asp.Versioning;
using CoreAlign.API.Common;
using CoreAlign.Application.Common;
using CoreAlign.Application.Customers.Commands;
using CoreAlign.Application.Customers.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CoreAlign.API.Controllers;

[ApiController]
[Authorize]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
public class CustomersController : ControllerBase
{
    private readonly IMediator _mediator;

    public CustomersController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<IActionResult> GetCustomersAsync(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null,
        [FromQuery] bool? isActive = null,
        CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(new GetCustomersQuery(page, pageSize, search, isActive), cancellationToken);
        return result.ToOk();
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetCustomerByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetCustomerByIdQuery(id), cancellationToken);
        return result.ToOk();
    }

    [HttpGet("{id:guid}/summary")]
    public async Task<IActionResult> GetCustomerSummaryAsync(Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetCustomerSummaryQuery(id), cancellationToken);
        return result.ToOk();
    }

    [HttpGet("{id:guid}/overview")]
    public async Task<IActionResult> GetCustomerOverviewAsync(Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetCustomerOverviewQuery(id), cancellationToken);
        return result.ToOk();
    }

    [HttpGet("{id:guid}/analytics")]
    public async Task<IActionResult> GetCustomerAnalyticsAsync(
        Guid id,
        [FromQuery] int monthsBack = 12,
        CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(new GetCustomerAnalyticsQuery(id, monthsBack), cancellationToken);
        return result.ToOk();
    }

    [HttpGet("{id:guid}/transactions")]
    public async Task<IActionResult> GetCustomerTransactionsAsync(
        Guid id,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(new GetCustomerTransactionsQuery(id, page, pageSize), cancellationToken);
        return result.ToOk();
    }

    [HttpGet("{id:guid}/addresses")]
    public async Task<IActionResult> GetCustomerAddressesAsync(Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetCustomerAddressesQuery(id), cancellationToken);
        return result.ToOk();
    }

    [HttpPost("{id:guid}/addresses")]
    public async Task<IActionResult> CreateCustomerAddressAsync(Guid id, [FromBody] CreateCustomerAddressCommand command, CancellationToken cancellationToken)
    {
        if (id != command.CustomerId)
        {
            return BadRequest(ApiResponse<object>.Failure("Route id does not match command id.", 400));
        }

        var result = await _mediator.Send(command, cancellationToken);
        return result.ToCreated();
    }

    [HttpPut("{id:guid}/addresses/{addressId:guid}")]
    public async Task<IActionResult> UpdateCustomerAddressAsync(Guid id, Guid addressId, [FromBody] UpdateCustomerAddressCommand command, CancellationToken cancellationToken)
    {
        if (id != command.CustomerId || addressId != command.Id)
        {
            return BadRequest(ApiResponse<object>.Failure("Route id does not match command id.", 400));
        }

        var result = await _mediator.Send(command, cancellationToken);
        return result.ToOk();
    }

    [HttpDelete("{id:guid}/addresses/{addressId:guid}")]
    public async Task<IActionResult> DeleteCustomerAddressAsync(Guid id, Guid addressId, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new DeleteCustomerAddressCommand(id, addressId), cancellationToken);
        return result.ToOk();
    }

    [HttpGet("{id:guid}/contacts")]
    public async Task<IActionResult> GetCustomerContactsAsync(Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetCustomerContactsQuery(id), cancellationToken);
        return result.ToOk();
    }

    [HttpPost("{id:guid}/contacts")]
    public async Task<IActionResult> CreateCustomerContactAsync(Guid id, [FromBody] CreateCustomerContactCommand command, CancellationToken cancellationToken)
    {
        if (id != command.CustomerId)
        {
            return BadRequest(ApiResponse<object>.Failure("Route id does not match command id.", 400));
        }

        var result = await _mediator.Send(command, cancellationToken);
        return result.ToCreated();
    }

    [HttpPut("{id:guid}/contacts/{contactId:guid}")]
    public async Task<IActionResult> UpdateCustomerContactAsync(Guid id, Guid contactId, [FromBody] UpdateCustomerContactCommand command, CancellationToken cancellationToken)
    {
        if (id != command.CustomerId || contactId != command.Id)
        {
            return BadRequest(ApiResponse<object>.Failure("Route id does not match command id.", 400));
        }

        var result = await _mediator.Send(command, cancellationToken);
        return result.ToOk();
    }

    [HttpDelete("{id:guid}/contacts/{contactId:guid}")]
    public async Task<IActionResult> DeleteCustomerContactAsync(Guid id, Guid contactId, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new DeleteCustomerContactCommand(id, contactId), cancellationToken);
        return result.ToOk();
    }

    [HttpPost]
    public async Task<IActionResult> CreateCustomerAsync([FromBody] CreateCustomerCommand command, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(command, cancellationToken);
        return result.ToCreated();
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> UpdateCustomerAsync(Guid id, [FromBody] UpdateCustomerCommand command, CancellationToken cancellationToken)
    {
        if (id != command.Id)
        {
            return BadRequest(ApiResponse<object>.Failure("Route id does not match command id.", 400));
        }

        var result = await _mediator.Send(command, cancellationToken);
        return result.ToOk();
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "TenantAdmin")]
    public async Task<IActionResult> DeleteCustomerAsync(Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new DeleteCustomerCommand(id), cancellationToken);
        return result.ToOk();
    }
}
