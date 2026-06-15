using Asp.Versioning;
using CoreAlign.API.Common;
using CoreAlign.Application.Providers.Commands;
using CoreAlign.Application.Providers.Queries;
using CoreAlign.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CoreAlign.API.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Authorize(Roles = "TenantAdmin")]
[Route("api/v{version:apiVersion}/admin/providers")]
public class TenantProviderConfigsController : ControllerBase
{
    private readonly IMediator _mediator;

    public TenantProviderConfigsController(IMediator mediator) => _mediator = mediator;

    [HttpGet]
    public async Task<IActionResult> List([FromQuery] ProviderCategory? category, CancellationToken ct) =>
        (await _mediator.Send(new GetTenantProviderConfigsQuery(category), ct)).ToOk();

    [HttpPut]
    public async Task<IActionResult> Upsert([FromBody] UpsertTenantProviderConfigCommand cmd, CancellationToken ct) =>
        (await _mediator.Send(cmd, ct)).ToOk();

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await _mediator.Send(new DeleteTenantProviderConfigCommand(id), ct);
        return NoContent();
    }
}
