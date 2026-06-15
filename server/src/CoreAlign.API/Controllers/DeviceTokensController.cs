using Asp.Versioning;
using CoreAlign.API.Common;
using CoreAlign.Application.Common;
using CoreAlign.Application.Notifications.DeviceTokens;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CoreAlign.API.Controllers;

[ApiController]
[Authorize]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/notifications/device-tokens")]
public class DeviceTokensController : ControllerBase
{
    private readonly IMediator _mediator;

    public DeviceTokensController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost]
    public async Task<IActionResult> Register(
        [FromBody] RegisterDeviceTokenRequest body,
        CancellationToken cancellationToken)
    {
        if (body is null || string.IsNullOrWhiteSpace(body.Token) || string.IsNullOrWhiteSpace(body.Platform))
        {
            return BadRequest(ApiResponse<object>.Failure("Token and platform are required.", 400));
        }

        var command = new RegisterDeviceTokenCommand(body.Token, body.Platform, body.DeviceName, body.OsVersion);
        var result = await _mediator.Send(command, cancellationToken);
        return result.ToCreated();
    }

    [HttpDelete("{token}")]
    public async Task<IActionResult> Unregister(string token, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return BadRequest(ApiResponse<object>.Failure("Token is required.", 400));
        }

        var command = new DeactivateDeviceTokenCommand(token);
        var changed = await _mediator.Send(command, cancellationToken);
        return changed ? NoContent() : NotFound();
    }
}
