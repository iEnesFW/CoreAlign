using Asp.Versioning;
using CoreAlign.Application.Whitelabel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CoreAlign.API.Controllers;

[ApiController]
[AllowAnonymous]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/public/theme")]
public class PublicThemeController : ControllerBase
{
    private readonly ITenantThemeService _service;

    public PublicThemeController(ITenantThemeService service) => _service = service;

    [HttpGet]
    public async Task<ActionResult<PublicTenantThemeDto>> GetAsync(
        [FromQuery] string? subdomain,
        [FromQuery] string? domain,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(subdomain) && string.IsNullOrWhiteSpace(domain))
        {
            return BadRequest(new { error = "subdomain or domain query parameter is required." });
        }

        PublicTenantThemeDto? dto = null;
        if (!string.IsNullOrWhiteSpace(subdomain))
        {
            dto = await _service.GetPublicThemeBySubdomainAsync(subdomain, ct);
        }
        if (dto is null && !string.IsNullOrWhiteSpace(domain))
        {
            dto = await _service.GetPublicThemeByCustomDomainAsync(domain, ct);
        }

        if (dto is null) return NotFound();
        return Ok(dto);
    }
}
