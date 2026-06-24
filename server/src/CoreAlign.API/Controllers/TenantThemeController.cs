using Asp.Versioning;
using CoreAlign.Application.Whitelabel;
using CoreAlign.Domain.Enums;
using CoreAlign.Domain.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CoreAlign.API.Controllers;

[ApiController]
[Authorize(Roles = "TenantAdmin")]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/admin/tenant-theme")]
public class TenantThemeController : ControllerBase
{
    private readonly ITenantThemeService _service;
    private readonly ITenantContext _tenantContext;

    public TenantThemeController(ITenantThemeService service, ITenantContext tenantContext)
    {
        _service = service;
        _tenantContext = tenantContext;
    }

    public sealed record UpdateTenantThemeRequest(
        string PrimaryColor,
        string AccentColor,
        string? BrandName,
        string? CustomSubdomain,
        string? CustomDomain,
        string EmailFromName,
        string? EmailFromAddress,
        string? LoginHeadingMd);

    [HttpGet]
    public async Task<ActionResult<TenantThemeDto>> GetAsync(CancellationToken ct)
    {
        var tenantId = _tenantContext.RequireTenantId();
        var dto = await _service.GetThemeAsync(tenantId, ct);
        return Ok(dto);
    }

    [HttpPut]
    public async Task<ActionResult<TenantThemeDto>> UpdateAsync([FromBody] UpdateTenantThemeRequest request, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);
        var tenantId = _tenantContext.RequireTenantId();
        var payload = new UpdateTenantThemePayload(
            request.PrimaryColor,
            request.AccentColor,
            request.BrandName,
            request.CustomSubdomain,
            request.CustomDomain,
            request.EmailFromName,
            request.EmailFromAddress,
            request.LoginHeadingMd);
        var dto = await _service.UpdateThemeAsync(tenantId, payload, ct);
        return Ok(dto);
    }

    [HttpPost("assets/{kind}")]
    [RequestSizeLimit(TenantThemeAssetPolicy.MaxBytes + (64 * 1024))]
    [Microsoft.AspNetCore.Mvc.ApiExplorerSettings(IgnoreApi = true)]
    public async Task<ActionResult<TenantThemeAssetDto>> UploadAssetAsync(
        TenantThemeAssetKind kind,
        [FromForm] IFormFile file,
        CancellationToken ct)
    {
        if (file is null || file.Length == 0)
        {
            return BadRequest(new { error = "File is required." });
        }

        var tenantId = _tenantContext.RequireTenantId();
        await using var stream = file.OpenReadStream();
        var dto = await _service.UploadAssetAsync(tenantId, kind, file.FileName, file.ContentType, stream, ct);
        return Ok(dto);
    }
}
