using Asp.Versioning;
using CoreAlign.API.Authorization;
using CoreAlign.API.Common;
using CoreAlign.Application.Common;
using CoreAlign.Application.Imports;
using CoreAlign.Application.Imports.Commands;
using MediatR;
using Microsoft.AspNetCore.Authorization;
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

    public ImportsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost("customers/preview")]
    [RequestSizeLimit(MaxBytes)]
    public async Task<IActionResult> PreviewCustomersAsync(IFormFile file, CancellationToken cancellationToken)
    {
        if (!TryResolveFormat(file, out var format, out var error))
        {
            return BadRequest(ApiResponse<object>.Failure(error, 400));
        }
        using var stream = file.OpenReadStream();
        var preview = await _mediator.Send(new PreviewCustomerImportCommand(stream, format), cancellationToken);
        return preview.ToOk();
    }

    [HttpPost("products/preview")]
    [RequestSizeLimit(MaxBytes)]
    public async Task<IActionResult> PreviewProductsAsync(IFormFile file, CancellationToken cancellationToken)
    {
        if (!TryResolveFormat(file, out var format, out var error))
        {
            return BadRequest(ApiResponse<object>.Failure(error, 400));
        }
        using var stream = file.OpenReadStream();
        var preview = await _mediator.Send(new PreviewProductImportCommand(stream, format), cancellationToken);
        return preview.ToOk();
    }

    [HttpPost("gl-accounts/preview")]
    [RequestSizeLimit(MaxBytes)]
    public async Task<IActionResult> PreviewGLAccountsAsync(IFormFile file, CancellationToken cancellationToken)
    {
        if (!TryResolveFormat(file, out var format, out var error))
        {
            return BadRequest(ApiResponse<object>.Failure(error, 400));
        }
        using var stream = file.OpenReadStream();
        var preview = await _mediator.Send(new PreviewGLAccountImportCommand(stream, format), cancellationToken);
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

    private static bool TryResolveFormat(IFormFile? file, out BulkImportFileFormat format, out string error)
    {
        format = BulkImportFileFormat.Csv;
        error = string.Empty;
        if (file is null || file.Length == 0)
        {
            error = "A non-empty file is required.";
            return false;
        }
        var ext = Path.GetExtension(file.FileName)?.ToLowerInvariant();
        if (ext == ".csv")
        {
            format = BulkImportFileFormat.Csv;
            return true;
        }
        if (ext == ".xlsx")
        {
            format = BulkImportFileFormat.Xlsx;
            return true;
        }
        error = "Only .csv and .xlsx files are supported.";
        return false;
    }

    public record CommitImportRequest(Guid SessionId, bool SkipInvalidRows);
}
