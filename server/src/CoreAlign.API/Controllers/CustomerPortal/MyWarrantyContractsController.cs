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
[Route("api/v{version:apiVersion}/customer-portal/warranty-contracts")]
public class MyWarrantyContractsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ICurrentCustomerAccessor _currentCustomer;

    public MyWarrantyContractsController(IMediator mediator, ICurrentCustomerAccessor currentCustomer)
    {
        _mediator = mediator;
        _currentCustomer = currentCustomer;
    }

    [HttpGet]
    public async Task<IActionResult> ListMy(CancellationToken ct)
    {
        var customerId = await _currentCustomer.GetCustomerIdOrThrowAsync(ct);
        return (await _mediator.Send(new ListWarrantyContractsForCustomerQuery(customerId), ct)).ToOk();
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetMy(Guid id, CancellationToken ct)
    {
        var customerId = await _currentCustomer.GetCustomerIdOrThrowAsync(ct);
        var contract = await _mediator.Send(new GetWarrantyContractByIdQuery(id), ct);
        if (contract is null || contract.CustomerId != customerId)
        {
            return NotFound(ApiResponse<object>.Failure("Warranty contract not found.", 404));
        }
        return contract.ToOk();
    }
}
