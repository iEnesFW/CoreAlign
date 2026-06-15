using Asp.Versioning;
using CoreAlign.API.Common;
using CoreAlign.Application.Common;
using CoreAlign.Application.Profile.Notifications;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CoreAlign.API.Controllers;

[ApiController]
[Authorize]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/profile/notification-preferences")]
public class ProfileNotificationsController : ControllerBase
{
    private readonly IMediator _mediator;

    public ProfileNotificationsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<IActionResult> List(CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new ListProfileNotificationPreferencesQuery(), cancellationToken);
        return result.ToOk();
    }

    [HttpPut]
    public async Task<IActionResult> Update(
        [FromBody] UpdateProfileNotificationPreferencesRequest body,
        CancellationToken cancellationToken)
    {
        if (body?.Items is null)
        {
            return BadRequest(ApiResponse<object>.Failure("Items list is required.", 400));
        }
        var command = new UpdateProfileNotificationPreferencesCommand(body.Items);
        var result = await _mediator.Send(command, cancellationToken);
        return result.ToOk();
    }
}

public sealed record UpdateProfileNotificationPreferencesRequest(
    IReadOnlyList<ProfileNotificationPreferenceItem> Items);
