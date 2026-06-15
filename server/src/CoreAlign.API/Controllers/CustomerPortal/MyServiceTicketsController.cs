using Asp.Versioning;
using CoreAlign.API.Common;
using CoreAlign.Application.Authorization;
using CoreAlign.Application.Common;
using CoreAlign.Application.Warranty;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CoreAlign.API.Controllers.CustomerPortal;

[ApiController]
[Authorize(Policy = CustomerPortalPolicies.SelfService)]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/customer-portal/service-tickets")]
public class MyServiceTicketsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ICurrentCustomerAccessor _currentCustomer;

    public MyServiceTicketsController(IMediator mediator, ICurrentCustomerAccessor currentCustomer)
    {
        _mediator = mediator;
        _currentCustomer = currentCustomer;
    }

    [HttpGet]
    public async Task<IActionResult> ListMy(CancellationToken ct)
    {
        var customerId = await _currentCustomer.GetCustomerIdOrThrowAsync(ct);
        return (await _mediator.Send(new ListMyServiceTicketsQuery(customerId), ct)).ToOk();
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetMy(Guid id, CancellationToken ct)
    {
        var customerId = await _currentCustomer.GetCustomerIdOrThrowAsync(ct);
        var tickets = await _mediator.Send(new ListMyServiceTicketsQuery(customerId), ct);
        var ticket = tickets.FirstOrDefault(t => t.Id == id);
        if (ticket is null)
        {
            return NotFound(ApiResponse<object>.Failure("Service ticket not found.", 404));
        }
        return ticket.ToOk();
    }

    [HttpPost]
    public async Task<IActionResult> CreateMy([FromBody] CustomerCreateServiceTicketRequest body, CancellationToken ct)
    {
        var customerId = await _currentCustomer.GetCustomerIdOrThrowAsync(ct);
        var command = new CreateServiceTicketCommand(
            customerId,
            body.Type,
            body.Priority,
            body.Title,
            body.DescriptionMd,
            body.WarrantyContractId);
        return (await _mediator.Send(command, ct)).ToCreated();
    }
}

public record CustomerCreateServiceTicketRequest(
    Domain.Enums.ServiceTicketType Type,
    Domain.Enums.ServiceTicketPriority Priority,
    string Title,
    string DescriptionMd,
    Guid? WarrantyContractId);
