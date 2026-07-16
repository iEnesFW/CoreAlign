using Asp.Versioning;
using CoreAlign.API.Common;
using CoreAlign.Application.GlassEnclosure.Authorization;
using CoreAlign.Application.GlassEnclosure.Commands;
using CoreAlign.Application.GlassEnclosure.DTOs;
using CoreAlign.Application.GlassEnclosure.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CoreAlign.API.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/glass-enclosure/project-templates")]
public class GlassProjectTemplatesController : ControllerBase
{
    private readonly IMediator _mediator;
    public GlassProjectTemplatesController(IMediator mediator) => _mediator = mediator;

    [HttpGet]
    [Authorize(Policy = GlassEnclosurePolicies.ProjectView)]
    public async Task<IActionResult> List(CancellationToken ct) =>
        (await _mediator.Send(new GetMyGlassProjectTemplatesQuery(), ct)).ToOk();

    [HttpGet("{id:guid}")]
    [Authorize(Policy = GlassEnclosurePolicies.ProjectView)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetGlassProjectTemplateByIdQuery(id), ct);
        return result is null ? NotFound() : result.ToOk();
    }

    [HttpPost]
    [Authorize(Policy = GlassEnclosurePolicies.ProjectCreate)]
    public async Task<IActionResult> Save([FromBody] SaveGlassProjectTemplateDto data, CancellationToken ct) =>
        (await _mediator.Send(new SaveGlassProjectTemplateCommand(data), ct)).ToCreated();

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = GlassEnclosurePolicies.ProjectCreate)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await _mediator.Send(new DeleteGlassProjectTemplateCommand(id), ct);
        return NoContent();
    }
}
