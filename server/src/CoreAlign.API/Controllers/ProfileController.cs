using Asp.Versioning;
using CoreAlign.API.Common;
using CoreAlign.Application.Identity.Locale;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CoreAlign.API.Controllers;

[ApiController]
[Authorize]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
public class ProfileController : ControllerBase
{
    private readonly IMediator _mediator;

    public ProfileController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPatch("locale")]
    public async Task<IActionResult> SetPreferredLocale(
        [FromBody] SetPreferredLocaleRequest body,
        CancellationToken cancellationToken)
    {
        var command = new SetPreferredLocaleCommand(body?.Locale ?? string.Empty);
        var result = await _mediator.Send(command, cancellationToken);
        return result.ToOk();
    }
}

public sealed record SetPreferredLocaleRequest(string Locale);
