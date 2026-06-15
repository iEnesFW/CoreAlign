using Asp.Versioning;
using CoreAlign.API.Common;
using CoreAlign.Application.Common.Storage;
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
    private readonly IFileStorage _fileStorage;
    private readonly IFieldSurveyRepository _surveyRepo;

    public GlassFieldSurveysController(
        IMediator mediator,
        IFileStorage fileStorage,
        IFieldSurveyRepository surveyRepo)
    {
        _mediator = mediator;
        _fileStorage = fileStorage;
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
        if (file is null || file.Length == 0) return BadRequest("Empty file");
        if (!AllowedImageContentTypes.Contains(file.ContentType?.ToLowerInvariant() ?? string.Empty))
        {
            return BadRequest("Unsupported content type");
        }
        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!AllowedImageExtensions.Contains(ext))
        {
            return BadRequest("Unsupported extension");
        }

        await using var stream = file.OpenReadStream();
        var sniffBuffer = new byte[12];
        var read = await stream.ReadAsync(sniffBuffer.AsMemory(0, sniffBuffer.Length), ct);
        if (!IsImageMagicBytes(sniffBuffer.AsSpan(0, read)))
        {
            return BadRequest("File content does not match an allowed image format");
        }
        stream.Position = 0;

        var safeName = $"{Guid.NewGuid():N}{ext}";
        var safeContentType = file.ContentType!.ToLowerInvariant();
        var scope = $"field-survey-photos/{survey.TenantId:N}/{survey.Id:N}";
        var stored = await _fileStorage.SaveAsync(
            scope,
            safeName,
            stream,
            safeContentType,
            ct);
        return new FieldSurveyUploadResultDto(stored.PublicUrl, stored.ContentType, stored.SizeBytes).ToCreated();
    }

    private static readonly HashSet<string> AllowedImageContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/jpeg",
        "image/png",
        "image/webp",
        "image/heic",
        "image/heif",
    };

    private static readonly HashSet<string> AllowedImageExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg", ".jpeg", ".png", ".webp", ".heic", ".heif",
    };

    private static readonly byte[][] HeifBrands =
    {
        new byte[] { (byte)'h', (byte)'e', (byte)'i', (byte)'c' },
        new byte[] { (byte)'h', (byte)'e', (byte)'i', (byte)'x' },
        new byte[] { (byte)'h', (byte)'e', (byte)'i', (byte)'m' },
        new byte[] { (byte)'h', (byte)'e', (byte)'i', (byte)'s' },
        new byte[] { (byte)'h', (byte)'e', (byte)'v', (byte)'c' },
        new byte[] { (byte)'m', (byte)'i', (byte)'f', (byte)'1' },
        new byte[] { (byte)'m', (byte)'s', (byte)'f', (byte)'1' },
    };

    private static bool IsImageMagicBytes(ReadOnlySpan<byte> bytes)
    {
        if (bytes.Length < 4) return false;
        if (bytes[0] == 0xFF && bytes[1] == 0xD8 && bytes[2] == 0xFF) return true;
        if (bytes.Length >= 8 &&
            bytes[0] == 0x89 && bytes[1] == 0x50 && bytes[2] == 0x4E && bytes[3] == 0x47 &&
            bytes[4] == 0x0D && bytes[5] == 0x0A && bytes[6] == 0x1A && bytes[7] == 0x0A) return true;
        if (bytes.Length >= 12 &&
            bytes[0] == 0x52 && bytes[1] == 0x49 && bytes[2] == 0x46 && bytes[3] == 0x46 &&
            bytes[8] == 0x57 && bytes[9] == 0x45 && bytes[10] == 0x42 && bytes[11] == 0x50) return true;
        if (bytes.Length >= 12 &&
            bytes[4] == 0x66 && bytes[5] == 0x74 && bytes[6] == 0x79 && bytes[7] == 0x70)
        {
            var brand = bytes.Slice(8, 4);
            foreach (var allowed in HeifBrands)
            {
                if (brand.SequenceEqual(allowed)) return true;
            }
        }
        return false;
    }
}
