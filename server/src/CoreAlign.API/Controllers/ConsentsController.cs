using Asp.Versioning;
using CoreAlign.API.Common;
using CoreAlign.Application.Consents;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CoreAlign.API.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/consents")]
public class ConsentsController : ControllerBase
{
    private readonly IMediator _mediator;

    public ConsentsController(IMediator mediator) => _mediator = mediator;

    public record CaptureConsentRequest(string Purpose, string Version, bool Given, string? Fingerprint);

    [HttpPost]
    [AllowAnonymous]
    public async Task<IActionResult> Capture([FromBody] CaptureConsentRequest body, CancellationToken ct)
    {
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
        var userAgent = Request.Headers.UserAgent.ToString();
        var command = new CaptureConsentCommand(
            body.Purpose,
            body.Version,
            body.Given,
            body.Fingerprint,
            ip,
            string.IsNullOrWhiteSpace(userAgent) ? null : userAgent);
        return (await _mediator.Send(command, ct)).ToCreated();
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<IActionResult> ListMine(CancellationToken ct) =>
        (await _mediator.Send(new ListMyConsentsQuery(), ct)).ToOk();

    [HttpPost("{id:guid}/withdraw")]
    [Authorize]
    public async Task<IActionResult> Withdraw([FromRoute] Guid id, CancellationToken ct) =>
        (await _mediator.Send(new WithdrawConsentCommand(id), ct)).ToOk();
}
