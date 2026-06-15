using Asp.Versioning;
using CoreAlign.API.Common;
using CoreAlign.Application.Authorization;
using CoreAlign.Application.Common;
using CoreAlign.Application.CustomerPortal.Payments;
using CoreAlign.Application.Payments.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CoreAlign.API.Controllers.CustomerPortal;

[ApiController]
[Authorize(Policy = CustomerPortalPolicies.SelfService)]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/customer-portal/payments")]
public class MyPaymentsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ICurrentCustomerAccessor _currentCustomer;

    public MyPaymentsController(IMediator mediator, ICurrentCustomerAccessor currentCustomer)
    {
        _mediator = mediator;
        _currentCustomer = currentCustomer;
    }

    [HttpGet]
    public async Task<IActionResult> ListMy(CancellationToken ct)
    {
        var customerId = await _currentCustomer.GetCustomerIdOrThrowAsync(ct);
        return (await _mediator.Send(new GetPaymentsByCustomerQuery(customerId), ct)).ToOk();
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetMy(Guid id, CancellationToken ct)
    {
        var customerId = await _currentCustomer.GetCustomerIdOrThrowAsync(ct);
        var payment = await _mediator.Send(new GetPaymentByIdQuery(id), ct);
        if (payment is null || payment.CustomerId != customerId)
        {
            return NotFound(ApiResponse<object>.Failure("Payment not found.", 404));
        }
        return payment.ToOk();
    }

    [HttpPost("initiate")]
    public async Task<IActionResult> Initiate(
        [FromBody] InitiateCustomerPaymentRequest body,
        CancellationToken ct)
    {
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
        var command = new InitiateInvoicePaymentCommand(body.InvoiceId, body.BillingInfo, ip, body.GatewayName);
        return (await _mediator.Send(command, ct)).ToOk();
    }
}

public record InitiateCustomerPaymentRequest(
    Guid InvoiceId,
    PortalBillingInfoInput? BillingInfo,
    string? GatewayName);
