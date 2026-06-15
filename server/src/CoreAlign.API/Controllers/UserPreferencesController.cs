using Asp.Versioning;
using CoreAlign.API.Common;
using CoreAlign.Application.Identity.PersonaPreference.Commands;
using CoreAlign.Application.Identity.PersonaPreference.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CoreAlign.API.Controllers;

[ApiController]
[Authorize]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/users/me/preferences")]
public class UserPreferencesController : ControllerBase
{
    private readonly IMediator _mediator;

    public UserPreferencesController(IMediator mediator) => _mediator = mediator;

    [HttpGet]
    public async Task<IActionResult> GetMine(CancellationToken ct) =>
        (await _mediator.Send(new GetCurrentUserPreferencesQuery(), ct)).ToOk();

    [HttpPatch]
    public async Task<IActionResult> Update([FromBody] SetUserPreferencesCommand cmd, CancellationToken ct) =>
        (await _mediator.Send(cmd, ct)).ToOk();
}
