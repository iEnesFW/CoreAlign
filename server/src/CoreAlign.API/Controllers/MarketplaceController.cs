using Asp.Versioning;
using CoreAlign.API.Common;
using CoreAlign.Application.GlassEnclosure.Authorization;
using CoreAlign.Application.GlassEnclosure.Marketplace.Commands;
using CoreAlign.Application.GlassEnclosure.Marketplace.DTOs;
using CoreAlign.Application.GlassEnclosure.Marketplace.Queries;
using CoreAlign.Domain.Enums;
using CoreAlign.Domain.Interfaces;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CoreAlign.API.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/marketplace")]
public class MarketplaceController : ControllerBase
{
    private readonly IMediator _mediator;

    public MarketplaceController(IMediator mediator) => _mediator = mediator;

    [HttpGet("templates")]
    [Authorize(Policy = GlassEnclosurePolicies.MarketplaceBrowse)]
    public async Task<IActionResult> List(
        [FromQuery] EnclosureCategory? category,
        [FromQuery] MarketplaceSortBy sortBy = MarketplaceSortBy.Popularity,
        [FromQuery] int skip = 0,
        [FromQuery] int take = 20,
        CancellationToken ct = default) =>
        (await _mediator.Send(new ListMarketplaceTemplatesQuery(category, sortBy, skip, take), ct)).ToOk();

    [HttpGet("templates/{id:guid}")]
    [Authorize(Policy = GlassEnclosurePolicies.MarketplaceBrowse)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct) =>
        (await _mediator.Send(new GetMarketplaceTemplateByIdQuery(id), ct)).ToOk();

    [HttpGet("templates/{id:guid}/reviews")]
    [Authorize(Policy = GlassEnclosurePolicies.MarketplaceBrowse)]
    public async Task<IActionResult> ListReviews(
        Guid id,
        [FromQuery] int skip = 0,
        [FromQuery] int take = 20,
        CancellationToken ct = default) =>
        (await _mediator.Send(new ListMarketplaceReviewsQuery(id, skip, take), ct)).ToOk();

    [HttpPost("templates/{id:guid}/install")]
    [Authorize(Policy = GlassEnclosurePolicies.MarketplaceInstall)]
    public async Task<IActionResult> Install(Guid id, CancellationToken ct) =>
        (await _mediator.Send(new InstallMarketplaceTemplateCommand(id), ct)).ToCreated();

    [HttpPost("templates/{id:guid}/review")]
    [Authorize(Policy = GlassEnclosurePolicies.MarketplaceReview)]
    public async Task<IActionResult> Review(Guid id, [FromBody] RateMarketplaceDto body, CancellationToken ct) =>
        (await _mediator.Send(new RateMarketplaceTemplateCommand(id, body.RatingStars, body.CommentMd), ct)).ToCreated();
}
