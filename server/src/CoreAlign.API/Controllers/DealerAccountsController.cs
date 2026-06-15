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
[Route("api/v{version:apiVersion}/dealer-accounts")]
public class DealerAccountsController : B2BControllerBase
{
    private readonly IMediator _mediator;
    public DealerAccountsController(IMediator mediator) => _mediator = mediator;

    [HttpGet]
    public async Task<IActionResult> List([FromQuery] Guid? customerId, CancellationToken ct)
        => (await _mediator.Send(new ListDealerAccountsQuery(customerId, CurrentUserId(), CurrentRoles()), ct)).ToOk();

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateDealerAccountCommand cmd, CancellationToken ct)
        => (await _mediator.Send(cmd with { CurrentUserId = CurrentUserId(), CurrentUserRoles = CurrentRoles() }, ct)).ToCreated();

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateDealerAccountCommand cmd, CancellationToken ct)
        => (await _mediator.Send(cmd with { Id = id, CurrentUserId = CurrentUserId(), CurrentUserRoles = CurrentRoles() }, ct)).ToOk();
}
