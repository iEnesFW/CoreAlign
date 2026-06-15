using Asp.Versioning;
using CoreAlign.API.Common;
using CoreAlign.Application.B2B;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CoreAlign.API.Controllers;

[ApiController]
[Authorize]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/dealer-customer-links")]
public class DealerCustomerLinksController : B2BControllerBase
{
    private readonly IMediator _mediator;
    public DealerCustomerLinksController(IMediator mediator) => _mediator = mediator;

    [HttpGet]
    public async Task<IActionResult> List([FromQuery] Guid? dealerAccountId, [FromQuery] Guid? customerId, CancellationToken ct)
        => (await _mediator.Send(new ListDealerCustomerLinksQuery(dealerAccountId, customerId, CurrentUserId(), CurrentRoles()), ct)).ToOk();

    [HttpPost]
    public async Task<IActionResult> Link([FromBody] LinkDealerToCustomerCommand cmd, CancellationToken ct)
        => (await _mediator.Send(cmd with { CurrentUserId = CurrentUserId(), CurrentUserRoles = CurrentRoles() }, ct)).ToCreated();

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Unlink(Guid id, [FromQuery] string? reason, CancellationToken ct)
        => (await _mediator.Send(new UnlinkDealerFromCustomerCommand(id, reason, CurrentUserId(), CurrentRoles()), ct)).ToOk();
}
