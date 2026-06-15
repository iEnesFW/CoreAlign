using Asp.Versioning;
using CoreAlign.API.Common;
using CoreAlign.Application.GlassEnclosure.Authorization;
using CoreAlign.Application.GlassEnclosure.Marketplace.Commands;
using CoreAlign.Application.GlassEnclosure.Marketplace.DTOs;
using CoreAlign.Application.GlassEnclosure.Marketplace.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CoreAlign.API.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/platform/marketplace")]
public class PlatformMarketplaceAdminController : ControllerBase
{
    private readonly IMediator _mediator;

    public PlatformMarketplaceAdminController(IMediator mediator) => _mediator = mediator;

    [HttpGet("pending")]
    [Authorize(Policy = GlassEnclosurePolicies.MarketplaceAdmin)]
    public async Task<IActionResult> ListPending(CancellationToken ct) =>
        (await _mediator.Send(new ListPendingMarketplaceSubmissionsQuery(), ct)).ToOk();

    [HttpPost("publish")]
    [Authorize(Policy = GlassEnclosurePolicies.MarketplaceAdmin)]
    public async Task<IActionResult> Publish([FromBody] PublishMarketplaceDto body, CancellationToken ct) =>
        (await _mediator.Send(new PublishMarketplaceCommand(body.TemplateId), ct)).ToOk();

    [HttpPost("reject")]
    [Authorize(Policy = GlassEnclosurePolicies.MarketplaceAdmin)]
    public async Task<IActionResult> Reject([FromBody] RejectMarketplaceDto body, CancellationToken ct) =>
        (await _mediator.Send(new RejectMarketplaceCommand(body.TemplateId, body.Reason), ct)).ToOk();
}
