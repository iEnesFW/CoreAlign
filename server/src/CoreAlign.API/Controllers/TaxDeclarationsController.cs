using Asp.Versioning;
using CoreAlign.API.Common;
using CoreAlign.Application.Tax.Commands;
using CoreAlign.Domain.Enums;
using CoreAlign.Domain.Exceptions;
using CoreAlign.Domain.Interfaces;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CoreAlign.API.Controllers;

[ApiController]
[Authorize(Roles = "TenantAdmin")]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/tax-declarations")]
public class TaxDeclarationsController : ControllerBase
{
    private readonly IMediator _mediator;

    public TaxDeclarationsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost("kdv1/build")]
    public async Task<IActionResult> BuildKdv1Async(
        [FromBody] BuildKdv1Request request,
        CancellationToken cancellationToken)
    {
        var id = await _mediator.Send(new BuildKdv1ForPeriodCommand(request.Year, request.Month), cancellationToken);
        return new { declarationId = id }.ToOk();
    }

    [HttpPost("babs/build")]
    public async Task<IActionResult> BuildBaBsAsync(
        [FromBody] BuildBaBsRequest request,
        CancellationToken cancellationToken)
    {
        var id = await _mediator.Send(new BuildBaBsForPeriodCommand(request.Year, request.Month), cancellationToken);
        return new { declarationId = id }.ToOk();
    }

    [HttpGet]
    public async Task<IActionResult> ListAsync(
        [FromQuery] int? year,
        [FromQuery] TaxDeclarationType? type,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new ListTaxDeclarationsQuery(year, type), cancellationToken);
        return result.ToOk();
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetTaxDeclarationByIdQuery(id), cancellationToken);
        return result.ToOk();
    }

    [HttpGet("{id:guid}/xml")]
    public async Task<IActionResult> GetXmlAsync(
        Guid id,
        [FromServices] ITaxDeclarationRepository repository,
        CancellationToken cancellationToken)
    {
        var declaration = await repository.GetByIdAsync(id, cancellationToken)
            ?? throw new TaxDeclarationNotFoundException();
        var xml = declaration.XmlPayload ?? string.Empty;
        var fileName = $"{declaration.DeclarationType}-{declaration.Year:D4}-{declaration.Month:D2}.xml";
        return File(System.Text.Encoding.UTF8.GetBytes(xml), "application/xml", fileName);
    }

    [HttpPost("{id:guid}/mark-submitted")]
    public async Task<IActionResult> MarkSubmittedAsync(
        Guid id,
        [FromBody] MarkSubmittedRequest? request,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new MarkTaxDeclarationSubmittedCommand(id, request?.SubmittedAtUtc),
            cancellationToken);
        return result.ToOk();
    }

    [HttpPost("{id:guid}/mark-accepted")]
    public async Task<IActionResult> MarkAcceptedAsync(Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new MarkTaxDeclarationAcceptedCommand(id), cancellationToken);
        return result.ToOk();
    }

    [HttpPost("{id:guid}/mark-rejected")]
    public async Task<IActionResult> MarkRejectedAsync(
        Guid id,
        [FromBody] MarkRejectedRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new MarkTaxDeclarationRejectedCommand(id, request.Reason),
            cancellationToken);
        return result.ToOk();
    }
}

public record BuildKdv1Request(int Year, int Month);
public record BuildBaBsRequest(int Year, int Month);
public record MarkSubmittedRequest(DateTime? SubmittedAtUtc);
public record MarkRejectedRequest(string Reason);
