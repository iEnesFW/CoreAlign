using Asp.Versioning;
using CoreAlign.API.Common;
using CoreAlign.Application.Authorization;
using CoreAlign.Application.Common;
using CoreAlign.Application.Sso;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CoreAlign.API.Controllers.Admin;

[ApiController]
[ApiVersion("1.0")]
[Authorize(Roles = AdminPolicies.TenantAdminRole)]
[Route("api/v{version:apiVersion}/admin/identity-providers")]
public class TenantIdentityProvidersController : ControllerBase
{
    private readonly ITenantIdentityProviderService _service;

    public TenantIdentityProvidersController(ITenantIdentityProviderService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<IActionResult> ListAsync(CancellationToken cancellationToken)
    {
        var list = await _service.ListAsync(cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<SsoIdentityProviderDto>>.Success(list));
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetAsync(Guid id, CancellationToken cancellationToken)
    {
        var dto = await _service.GetAsync(id, cancellationToken);
        return dto is null
            ? NotFound(ApiResponse<object>.Failure("Identity provider not found.", 404))
            : Ok(ApiResponse<SsoIdentityProviderDto>.Success(dto));
    }

    [HttpPost]
    public async Task<IActionResult> CreateAsync([FromBody] CreateSsoIdentityProviderRequest request, CancellationToken cancellationToken)
    {
        var created = await _service.CreateAsync(request, cancellationToken);
        return Ok(ApiResponse<SsoIdentityProviderDto>.Success(created));
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> UpdateAsync(Guid id, [FromBody] UpdateSsoIdentityProviderRequest request, CancellationToken cancellationToken)
    {
        var updated = await _service.UpdateAsync(id, request, cancellationToken);
        return Ok(ApiResponse<SsoIdentityProviderDto>.Success(updated));
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        await _service.DeleteAsync(id, cancellationToken);
        return Ok(ApiResponse<object>.Success(new { id }));
    }

    [HttpPost("{id:guid}/test-connection")]
    public async Task<IActionResult> TestAsync(Guid id, CancellationToken cancellationToken)
    {
        var result = await _service.TestConnectionAsync(id, cancellationToken);
        return Ok(ApiResponse<SsoTestConnectionResult>.Success(result));
    }
}
