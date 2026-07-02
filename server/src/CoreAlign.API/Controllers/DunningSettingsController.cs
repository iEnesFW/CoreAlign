using Asp.Versioning;
using CoreAlign.API.Common;
using CoreAlign.Application.Dunning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CoreAlign.API.Controllers;

[ApiController]
[Authorize(Roles = "TenantAdmin")]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/dunning-settings")]
public class DunningSettingsController : ControllerBase
{
    private readonly IMediator _mediator;
    public DunningSettingsController(IMediator mediator) => _mediator = mediator;

    [HttpGet]
    public async Task<IActionResult> List(CancellationToken ct)
        => (await _mediator.Send(new ListDunningSettingsQuery(), ct)).ToOk();

    [HttpPut]
    public async Task<IActionResult> Upsert([FromBody] UpsertDunningSettingCommand cmd, CancellationToken ct)
        => (await _mediator.Send(cmd, ct)).ToOk();
}
