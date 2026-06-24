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

namespace CoreAlign.API.Controllers;

[ApiController]
[Authorize]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/feedback")]
public class FeedbackController : ControllerBase
{
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

    [HttpGet]
    public async Task<IActionResult> List([FromQuery] FeedbackStatus? status, [FromQuery] FeedbackType? type, CancellationToken ct)
        => (await _mediator.Send(new ListFeedbackQuery(status, type), ct)).ToOk();

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
        => (await _mediator.Send(new GetFeedbackByIdQuery(id), ct)).ToOk();

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateFeedbackCommand cmd, CancellationToken ct)
    {
        // The submitter's name is taken from the authenticated identity, never the
        // request body, so it cannot be spoofed.
        var withUser = cmd with { CreatedByName = User.Identity?.Name };
        return (await _mediator.Send(withUser, ct)).ToCreated();
    }

    [HttpPut("{id:guid}/status")]
    [Authorize(Roles = "TenantAdmin")]
    public async Task<IActionResult> UpdateStatus(Guid id, [FromBody] UpdateFeedbackStatusCommand cmd, CancellationToken ct)
        => id != cmd.Id ? RouteIdMismatch() : (await _mediator.Send(cmd, ct)).ToOk();

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
