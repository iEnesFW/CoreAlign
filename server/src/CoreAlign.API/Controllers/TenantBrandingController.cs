using Asp.Versioning;
using CoreAlign.API.Common;
using CoreAlign.Application.Common;
using CoreAlign.Application.Tenants.Logo;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CoreAlign.API.Controllers;

[ApiController]
[Authorize(Roles = "TenantAdmin")]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/tenants/me")]
public class TenantBrandingController : ControllerBase
{
    private readonly IMediator _mediator;

    public TenantBrandingController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost("logo")]
    [RequestSizeLimit(TenantLogoPolicy.MaxBytes + (64 * 1024))]
    [Microsoft.AspNetCore.Mvc.ApiExplorerSettings(IgnoreApi = true)]
    public async Task<IActionResult> UploadLogo(
        [FromForm] IFormFile file,
        CancellationToken cancellationToken)
    {
        if (file is null || file.Length == 0)
        {
            return BadRequest(ApiResponse<object>.Failure("File is required.", 400));
        }

        await using var stream = file.OpenReadStream();
        var command = new UploadTenantLogoCommand(
            file.FileName,
            file.ContentType,
            file.Length,
            stream);
        var result = await _mediator.Send(command, cancellationToken);
        return result.ToOk();
    }
}
