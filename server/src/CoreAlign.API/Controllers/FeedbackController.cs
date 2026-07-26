using Asp.Versioning;
using CoreAlign.API.Common;
using CoreAlign.Application.Common;
using CoreAlign.Application.Common.Storage;
using CoreAlign.Application.Common.Upload;
using CoreAlign.Application.Feedback;
using CoreAlign.Domain.Enums;
using CoreAlign.Domain.Exceptions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace CoreAlign.API.Controllers;

public record UpdateFeedbackStatusRequest(CoreAlign.Domain.Enums.FeedbackStatus Status, string? AdminResponse);

public record AddFeedbackCommentRequest(string Body, bool IsInternal = false);

[ApiController]
[Authorize]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/feedback")]
public class FeedbackController : ControllerBase
{
    private const string AdminRoles = "TenantAdmin,PlatformAdmin";

    private readonly IMediator _mediator;
    private readonly IFileUploadService _uploads;
    private readonly IFileStorage _storage;

    public FeedbackController(IMediator mediator, IFileUploadService uploads, IFileStorage storage)
    {
        _mediator = mediator;
        _uploads = uploads;
        _storage = storage;
    }

    private static IActionResult RouteIdMismatch() =>
        new BadRequestObjectResult(ApiResponse<object>.Failure("Route id does not match command id.", 400));

    private Guid CurrentUserId =>
        Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : Guid.Empty;

    private bool IsPlatformAdmin => User.IsInRole("PlatformAdmin");

    [HttpGet]
    public async Task<IActionResult> List([FromQuery] FeedbackStatus? status, [FromQuery] FeedbackType? type, CancellationToken ct)
        => (await _mediator.Send(new ListFeedbackQuery(status, type), ct)).ToOk();

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
        => (await _mediator.Send(new GetFeedbackByIdQuery(id), ct)).ToOk();

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateFeedbackCommand cmd, CancellationToken ct)
    {
        // Identity comes from the token, never the request body, so it cannot be spoofed — and the
        // reporter id is what every later status/comment notification is addressed to.
        var withUser = cmd with { CreatedByName = User.Identity?.Name, CreatedByUserId = CurrentUserId };
        return (await _mediator.Send(withUser, ct)).ToCreated();
    }

    [HttpPut("{id:guid}/status")]
    [Authorize(Roles = AdminRoles)]
    public async Task<IActionResult> UpdateStatus(Guid id, [FromBody] UpdateFeedbackStatusCommand cmd, CancellationToken ct)
        => id != cmd.Id ? RouteIdMismatch() : (await _mediator.Send(cmd, ct)).ToOk();

    [HttpPatch("{id:guid}/status")]
    [Authorize(Roles = AdminRoles)]
    public async Task<IActionResult> PatchStatus(
        Guid id,
        [FromBody] UpdateFeedbackStatusRequest body,
        CancellationToken ct)
        => (await _mediator.Send(new UpdateFeedbackStatusCommand(id, body.Status, body.AdminResponse), ct))
            .ToOk();

    [HttpGet("{id:guid}/comments")]
    public async Task<IActionResult> ListComments(Guid id, CancellationToken ct)
        => (await _mediator.Send(new ListFeedbackCommentsQuery(id, IsPlatformAdmin), ct)).ToOk();

    [HttpPost("{id:guid}/comments")]
    public async Task<IActionResult> AddComment(
        Guid id,
        [FromBody] AddFeedbackCommentRequest body,
        CancellationToken ct)
        => (await _mediator.Send(
                new AddFeedbackCommentCommand(
                    id,
                    body.Body,
                    CurrentUserId,
                    User.Identity?.Name,
                    body.IsInternal,
                    IsPlatformAdmin),
                ct))
            .ToCreated();

    [HttpPost("{id:guid}/attachments")]
    [RequestSizeLimit(26 * 1024 * 1024)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> UploadAttachments(Guid id, IFormFileCollection files, CancellationToken ct)
    {
        if (files is null || files.Count == 0)
        {
            throw new FileUploadValidationException("At least one file is required.");
        }

        var uploaded = new List<FeedbackUploadedFile>(files.Count);
        foreach (var file in files)
        {
            await using var stream = file.OpenReadStream();
            var stored = await _uploads.UploadAsync(
                new FileUploadRequest(
                    stream,
                    file.FileName,
                    file.ContentType,
                    "attachment",
                    $"feedback-attachments/{id:N}"),
                ct);
            uploaded.Add(
                new FeedbackUploadedFile(
                    stored.RelativePath,
                    file.FileName,
                    stored.ContentType,
                    stored.SizeBytes));
        }

        var dto = await _mediator.Send(new AddFeedbackAttachmentsCommand(id, uploaded, CurrentUserId), ct);
        return dto.ToOk();
    }

    [HttpGet("{id:guid}/attachments")]
    public async Task<IActionResult> ListAttachments(Guid id, CancellationToken ct)
        => (await _mediator.Send(new ListFeedbackAttachmentsQuery(id), ct)).ToOk();

    [HttpGet("{id:guid}/attachments/{attachmentId:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DownloadAttachmentById(Guid id, Guid attachmentId, CancellationToken ct)
    {
        var descriptor = await _mediator.Send(new GetFeedbackAttachmentFileQuery(id, attachmentId), ct);
        if (descriptor is null)
        {
            return NotFound();
        }

        var stream = await _storage.OpenReadAsync(descriptor.RelativePath, ct);
        return File(stream, descriptor.ContentType, descriptor.FileName);
    }

    [HttpDelete("{id:guid}/attachments/{attachmentId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteAttachment(Guid id, Guid attachmentId, CancellationToken ct)
    {
        await _mediator.Send(new DeleteFeedbackAttachmentCommand(id, attachmentId), ct);
        return NoContent();
    }

    [HttpPost("{id:guid}/attachment")]
    [RequestSizeLimit(6 * 1024 * 1024)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> UploadAttachment(Guid id, IFormFile file, CancellationToken ct)
    {
        if (file is null || file.Length == 0)
        {
            throw new FileUploadValidationException("A file is required.");
        }

        await using var stream = file.OpenReadStream();
        var uploaded = await _uploads.UploadAsync(
            new FileUploadRequest(stream, file.FileName, file.ContentType, "attachment", $"feedback-attachments/{id:N}"),
            ct);
        var dto = await _mediator.Send(
            new AttachFeedbackFileCommand(id, uploaded.RelativePath, uploaded.FileName, uploaded.ContentType), ct);
        return dto.ToOk();
    }

    [HttpGet("{id:guid}/attachment")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DownloadAttachment(Guid id, CancellationToken ct)
    {
        var descriptor = await _mediator.Send(new GetFeedbackAttachmentQuery(id), ct);
        if (descriptor is null)
        {
            return NotFound();
        }

        var stream = await _storage.OpenReadAsync(descriptor.RelativePath, ct);
        return File(stream, descriptor.ContentType, descriptor.FileName);
    }
}
