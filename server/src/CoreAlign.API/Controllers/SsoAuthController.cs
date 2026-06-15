using Asp.Versioning;
using CoreAlign.API.Common;
using CoreAlign.Application.Common;
using CoreAlign.Application.Sso;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace CoreAlign.API.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/auth")]
public class SsoAuthController : ControllerBase
{
    private readonly ISsoLoginService _ssoLoginService;

    public SsoAuthController(ISsoLoginService ssoLoginService)
    {
        _ssoLoginService = ssoLoginService;
    }

    private IActionResult RedirectToFrontendError(string returnUrl, string errorCode)
    {
        if (Uri.TryCreate(returnUrl, UriKind.Absolute, out var uri))
        {
            var frontendLoginUrl = $"{uri.Scheme}://{uri.Authority}/login?error={errorCode}";
            return Redirect(frontendLoginUrl);
        }
        return BadRequest(new { error = errorCode });
    }

    [HttpGet("saml/{tenantSlug}/{idpName}/login")]
    [EnableRateLimiting("auth")]
    public async Task<IActionResult> SamlLoginAsync(
        string tenantSlug,
        string idpName,
        [FromQuery] string returnUrl,
        CancellationToken cancellationToken)
    {
        try
        {
            var url = await _ssoLoginService.BuildSamlRedirectUrlAsync(tenantSlug, idpName, returnUrl, cancellationToken);
            return Redirect(url);
        }
        catch (CoreAlign.Domain.Exceptions.DomainException)
        {
            return RedirectToFrontendError(returnUrl, "InvalidSsoProviderOrTenant");
        }
        catch (Exception)
        {
            return RedirectToFrontendError(returnUrl, "SsoConfigurationError");
        }
    }

    [HttpPost("saml/{tenantSlug}/{idpName}/acs")]
    [EnableRateLimiting("auth")]
    public async Task<IActionResult> SamlAcsAsync(
        string tenantSlug,
        string idpName,
        [FromBody] SamlAssertionRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var assertion = new SsoAssertionContext(
            request.NameId,
            request.Email,
            request.FirstName,
            request.LastName,
            request.Claims ?? new Dictionary<string, string>());
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        var userAgent = Request.Headers.UserAgent.ToString();
        var result = await _ssoLoginService.CompleteSamlLoginAsync(tenantSlug, idpName, assertion, ipAddress, userAgent, cancellationToken);
        return Ok(ApiResponse<SsoLoginResult>.Success(result));
    }

    [HttpGet("oidc/{tenantSlug}/{idpName}/login")]
    [EnableRateLimiting("auth")]
    public async Task<IActionResult> OidcLoginAsync(
        string tenantSlug,
        string idpName,
        [FromQuery] string returnUrl,
        CancellationToken cancellationToken)
    {
        try
        {
            var state = Guid.NewGuid().ToString("N");
            var url = await _ssoLoginService.BuildOidcAuthorizeUrlAsync(tenantSlug, idpName, returnUrl, state, cancellationToken);
            return Redirect(url);
        }
        catch (CoreAlign.Domain.Exceptions.DomainException)
        {
            return RedirectToFrontendError(returnUrl, "InvalidSsoProviderOrTenant");
        }
        catch (Exception)
        {
            return RedirectToFrontendError(returnUrl, "SsoConfigurationError");
        }
    }

    [HttpPost("oidc/{tenantSlug}/{idpName}/callback")]
    [EnableRateLimiting("auth")]
    public async Task<IActionResult> OidcCallbackAsync(
        string tenantSlug,
        string idpName,
        [FromBody] OidcCallbackRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var assertion = new SsoAssertionContext(
            request.Subject,
            request.Email,
            request.FirstName,
            request.LastName,
            request.Claims ?? new Dictionary<string, string>());
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        var userAgent = Request.Headers.UserAgent.ToString();
        var result = await _ssoLoginService.CompleteOidcLoginAsync(tenantSlug, idpName, assertion, ipAddress, userAgent, cancellationToken);
        return Ok(ApiResponse<SsoLoginResult>.Success(result));
    }
}

public record SamlAssertionRequest(
    string NameId,
    string Email,
    string? FirstName,
    string? LastName,
    Dictionary<string, string>? Claims);

public record OidcCallbackRequest(
    string Subject,
    string Email,
    string? FirstName,
    string? LastName,
    Dictionary<string, string>? Claims);
