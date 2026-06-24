using Asp.Versioning;
using CoreAlign.API.Common;
using CoreAlign.Application.Common.Upload;
using CoreAlign.Application.GlassEnclosure.Authorization;
using CoreAlign.Application.GlassEnclosure.Commands;
using CoreAlign.Application.GlassEnclosure.DTOs;
using CoreAlign.Domain.Exceptions;
using CoreAlign.Domain.Interfaces;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace CoreAlign.API.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/glass-enclosure/field-surveys")]
public class GlassFieldSurveysController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IFileUploadService _uploads;
    private readonly IFieldSurveyRepository _surveyRepo;

    public GlassFieldSurveysController(
        IMediator mediator,
        IFileUploadService uploads,
        IFieldSurveyRepository surveyRepo)
    {
        _mediator = mediator;
        _uploads = uploads;
        _surveyRepo = surveyRepo;
    }

    [HttpGet("by-project/{projectId:guid}")]
    [Authorize(Policy = GlassEnclosurePolicies.DesignerOpen)]
    public async Task<IActionResult> ListByProject(Guid projectId, CancellationToken ct) =>
        (await _mediator.Send(new GetFieldSurveysByProjectQuery(projectId), ct)).ToOk();

    [HttpGet("{id:guid}")]
    [Authorize(Policy = GlassEnclosurePolicies.DesignerOpen)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct) =>
        (await _mediator.Send(new GetFieldSurveyByIdQuery(id), ct)).ToOk();

    [HttpPost]
    [Authorize(Policy = GlassEnclosurePolicies.FieldSurveyCreate)]
    public async Task<IActionResult> Create([FromBody] CreateFieldSurveyDto data, CancellationToken ct) =>
        (await _mediator.Send(new CreateFieldSurveyCommand(data), ct)).ToCreated();

    [HttpPut("{id:guid}")]
    [Authorize(Policy = GlassEnclosurePolicies.FieldSurveyCreate)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateFieldSurveyDto data, CancellationToken ct) =>
        (await _mediator.Send(new UpdateFieldSurveyCommand(id, data), ct)).ToOk();

    [HttpPost("{id:guid}/submit")]
    [Authorize(Policy = GlassEnclosurePolicies.FieldSurveySubmit)]
    public async Task<IActionResult> Submit(Guid id, CancellationToken ct) =>
        (await _mediator.Send(new SubmitFieldSurveyCommand(id), ct)).ToOk();

    [HttpPost("{id:guid}/approve")]
    [Authorize(Policy = GlassEnclosurePolicies.FieldSurveyApprove)]
    public async Task<IActionResult> Approve(Guid id, [FromBody] ApproveFieldSurveyDto data, CancellationToken ct) =>
        (await _mediator.Send(new ApproveFieldSurveyCommand(id, data), ct)).ToOk();

    [HttpPost("{id:guid}/reject")]
    [Authorize(Policy = GlassEnclosurePolicies.FieldSurveyApprove)]
    public async Task<IActionResult> Reject(Guid id, [FromBody] RejectFieldSurveyDto data, CancellationToken ct) =>
        (await _mediator.Send(new RejectFieldSurveyCommand(id, data), ct)).ToOk();

    [HttpPost("{id:guid}/apply")]
    [Authorize(Policy = GlassEnclosurePolicies.FieldSurveyApprove)]
    public async Task<IActionResult> Apply(Guid id, CancellationToken ct) =>
        (await _mediator.Send(new ApplyFieldSurveyCommand(id), ct)).ToOk();

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = GlassEnclosurePolicies.FieldSurveyCreate)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await _mediator.Send(new DeleteFieldSurveyCommand(id), ct);
        return NoContent();
    }

    [HttpPost("{surveyId:guid}/photos")]
    [Authorize(Policy = GlassEnclosurePolicies.FieldSurveyCreate)]
    [EnableRateLimiting("upload")]
    [RequestSizeLimit(25 * 1024 * 1024)]
    [Microsoft.AspNetCore.Mvc.ApiExplorerSettings(IgnoreApi = true)]
    public async Task<IActionResult> UploadPhoto(Guid surveyId, [FromForm] IFormFile file, CancellationToken ct)
    {
        var survey = await _surveyRepo.GetByIdAsync(surveyId, ct)
            ?? throw new GlassEnclosureNotFoundException("FieldSurvey");
        if (file is null || file.Length == 0)
        {
            throw new FileUploadValidationException("A non-empty file is required.");
        }

        await using var stream = file.OpenReadStream();
        var uploaded = await _uploads.UploadAsync(
            new FileUploadRequest(
                stream,
                file.FileName,
                file.ContentType,
                FileUploadProfiles.GlassPhoto.Name,
                $"field-survey-photos/{survey.TenantId:N}/{survey.Id:N}"),
            ct);
        return new FieldSurveyUploadResultDto(uploaded.PublicUrl, uploaded.ContentType, uploaded.SizeBytes).ToCreated();
    }
}
