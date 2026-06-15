using Asp.Versioning;
using CoreAlign.API.Common;
using CoreAlign.Application.Privacy;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CoreAlign.API.Controllers;

[ApiController]
[Authorize]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/privacy")]
public class PrivacyController : ControllerBase
{
    private readonly IMediator _mediator;

    public PrivacyController(IMediator mediator) => _mediator = mediator;

    [HttpGet("me/export")]
    public async Task<IActionResult> ExportMyData(CancellationToken ct) =>
        (await _mediator.Send(new ExportMyDataQuery(), ct)).ToOk();

    [HttpPost("me/erase")]
    public async Task<IActionResult> EraseMyAccount([FromBody] EraseMyAccountCommand command, CancellationToken ct) =>
        (await _mediator.Send(command, ct)).ToOk();

    [HttpPost("customers/{customerId:guid}/erase")]
    [Authorize(Roles = "TenantAdmin")]
    [Authorization.RequireRecentMfa]
    public async Task<IActionResult> EraseCustomerByAdmin(
        Guid customerId,
        [FromBody] EraseCustomerByAdminBody body,
        CancellationToken ct) =>
        (await _mediator.Send(new EraseCustomerByAdminCommand(customerId, body.ConfirmationUsername), ct)).ToOk();
}

public record EraseCustomerByAdminBody(string ConfirmationUsername);
