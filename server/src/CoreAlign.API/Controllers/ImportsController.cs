using Asp.Versioning;
using CoreAlign.API.Authorization;
using CoreAlign.API.Common;
using CoreAlign.Application.Common;
using CoreAlign.Application.Common.Upload;
using CoreAlign.Application.Imports;
using CoreAlign.Application.Imports.Commands;
using CoreAlign.Domain.Exceptions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace CoreAlign.API.Controllers;

[ApiController]
[Authorize(Policy = PersonaPolicies.Tenant)]
[Authorize(Roles = "TenantAdmin")]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/imports")]
public class ImportsController : ControllerBase
{
    private const long MaxBytes = 10 * 1024 * 1024;
    private readonly IMediator _mediator;
    private readonly IFileUploadService _uploads;

    public ImportsController(IMediator mediator, IFileUploadService uploads)
    {
        _mediator = mediator;
        _uploads = uploads;
    }

    [HttpPost("customers/preview")]
    [RequestSizeLimit(MaxBytes)]
    public async Task<IActionResult> PreviewCustomersAsync(IFormFile file, CancellationToken cancellationToken)
    {
        using var validated = await ValidateImportFileAsync(file, cancellationToken);
        var preview = await _mediator.Send(
            new PreviewCustomerImportCommand(validated.Content, MapFormat(validated.DetectedType)),
            cancellationToken);
        return preview.ToOk();
    }

    [HttpPost("products/preview")]
    [RequestSizeLimit(MaxBytes)]
    public async Task<IActionResult> PreviewProductsAsync(IFormFile file, CancellationToken cancellationToken)
    {
        using var validated = await ValidateImportFileAsync(file, cancellationToken);
        var preview = await _mediator.Send(
            new PreviewProductImportCommand(validated.Content, MapFormat(validated.DetectedType)),
            cancellationToken);
        return preview.ToOk();
    }

    [HttpPost("gl-accounts/preview")]
    [RequestSizeLimit(MaxBytes)]
    public async Task<IActionResult> PreviewGLAccountsAsync(IFormFile file, CancellationToken cancellationToken)
    {
        using var validated = await ValidateImportFileAsync(file, cancellationToken);
        var preview = await _mediator.Send(
            new PreviewGLAccountImportCommand(validated.Content, MapFormat(validated.DetectedType)),
            cancellationToken);
        return preview.ToOk();
    }

    [HttpPost("{entityKind}/commit")]
    public async Task<IActionResult> CommitAsync(
        string entityKind,
        [FromBody] CommitImportRequest body,
        CancellationToken cancellationToken)
    {
        if (body.SessionId == Guid.Empty)
        {
            return BadRequest(ApiResponse<object>.Failure("SessionId is required.", 400));
        }
        var result = await _mediator.Send(new CommitImportCommand(entityKind, body.SessionId, body.SkipInvalidRows), cancellationToken);
        return result.ToOk();
    }

    private async Task<ValidatedFile> ValidateImportFileAsync(IFormFile? file, CancellationToken cancellationToken)
    {
        if (file is null || file.Length == 0)
        {
            throw new FileUploadValidationException("A non-empty file is required.");
        }

        await using var source = file.OpenReadStream();
        return await _uploads.ValidateAsync(
            new FileValidationRequest(source, file.FileName, file.ContentType, FileUploadProfiles.Import.Name),
            cancellationToken);
    }

    private static BulkImportFileFormat MapFormat(DetectedFileType detected) => detected switch
    {
        DetectedFileType.Csv => BulkImportFileFormat.Csv,
        DetectedFileType.Zip => BulkImportFileFormat.Xlsx,
        _ => throw new FileUploadValidationException("Only .csv and .xlsx files are supported."),
    };

    public record CommitImportRequest(Guid SessionId, bool SkipInvalidRows);
}
