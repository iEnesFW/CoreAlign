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
[Route("api/v{version:apiVersion}/my-marketplace-submissions")]
public class MyMarketplaceSubmissionsController : ControllerBase
{
    private readonly IMediator _mediator;

    public MyMarketplaceSubmissionsController(IMediator mediator) => _mediator = mediator;

    [HttpGet]
    [Authorize(Policy = GlassEnclosurePolicies.MarketplaceSubmit)]
    public async Task<IActionResult> List(CancellationToken ct) =>
        (await _mediator.Send(new ListMyMarketplaceSubmissionsQuery(), ct)).ToOk();

    [HttpPost]
    [Authorize(Policy = GlassEnclosurePolicies.MarketplaceSubmit)]
    public async Task<IActionResult> Submit([FromBody] SubmitMarketplaceDto body, CancellationToken ct) =>
        (await _mediator.Send(new SubmitToMarketplaceCommand(body.TenantTemplateId), ct)).ToCreated();
}
