using System.Security.Claims;
using CoreAlign.Application.B2B;
using CoreAlign.Domain.Exceptions;
using Microsoft.AspNetCore.Http;

namespace CoreAlign.Infrastructure.Services;

/// <summary>
/// Resolves the authenticated user id from the JWT <c>sub</c> claim (with a
/// <see cref="ClaimTypes.NameIdentifier"/> fallback for unit-test principals).
/// Mirrors <c>PortalScopeService</c>'s resolver but is reused across handlers
/// that need only the user id, not the customer/dealer scope.
/// </summary>
public sealed class CurrentUserAccessor : ICurrentUserAccessor
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserAccessor(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public Guid? UserId
    {
        get
        {
            var principal = _httpContextAccessor.HttpContext?.User;
            if (principal?.Identity?.IsAuthenticated != true) return null;
            var raw = principal.FindFirstValue("sub")
                      ?? principal.FindFirstValue(ClaimTypes.NameIdentifier);
            return Guid.TryParse(raw, out var id) && id != Guid.Empty ? id : (Guid?)null;
        }
    }

    public Guid UserIdOrThrow() =>
        UserId ?? throw new PortalScopeNotResolvedException(
            "Current user id is missing or malformed in the request claims.");
}
