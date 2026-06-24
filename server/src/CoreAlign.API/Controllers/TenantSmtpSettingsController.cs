using Asp.Versioning;
using CoreAlign.API.Common;
using CoreAlign.Application.Notifications.Smtp;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CoreAlign.API.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Authorize(Roles = "TenantAdmin")]
[Route("api/v{version:apiVersion}/admin/notifications/smtp")]
public class TenantSmtpSettingsController : ControllerBase
{
    private readonly IMediator _mediator;

    public TenantSmtpSettingsController(IMediator mediator) => _mediator = mediator;

    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken ct) =>
        (await _mediator.Send(new GetTenantSmtpSettingsQuery(), ct)).ToOk();

    [HttpPut]
    public async Task<IActionResult> Upsert([FromBody] UpsertTenantSmtpSettingsCommand cmd, CancellationToken ct) =>
        (await _mediator.Send(cmd, ct)).ToOk();

    [HttpPost("test")]
    public async Task<IActionResult> Test([FromBody] SendTestEmailCommand cmd, CancellationToken ct) =>
        (await _mediator.Send(cmd, ct)).ToOk();

    [HttpGet("health")]
    public async Task<IActionResult> Health(CancellationToken ct) =>
        (await _mediator.Send(new CheckSmtpHealthQuery(), ct)).ToOk();
}
